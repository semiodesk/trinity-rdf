// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Semiodesk.Trinity.Generator
{
    /// <summary>
    /// Emits the RDF mapping members historically produced by the cilg IL weaver:
    /// for each <c>partial</c> property annotated with <c>[RdfProperty]</c> a backing
    /// <c>PropertyMapping&lt;T&gt;</c> field plus the implementing getter/setter, and a
    /// <c>GetTypes()</c> override for every <c>partial</c> class annotated with <c>[RdfClass]</c>.
    ///
    /// Only <c>partial</c> declarations are processed, so this coexists with the legacy weaver
    /// (which handles plain auto-property classes) during migration. The weaver additionally
    /// defers to any pre-existing GetTypes / non-auto accessors, so a migrated (partial) class
    /// is left untouched by the weaver even when both run.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class MappingSourceGenerator : IIncrementalGenerator
    {
        private const string RdfPropertyAttribute = "Semiodesk.Trinity.RdfPropertyAttribute";
        private const string RdfClassAttribute = "Semiodesk.Trinity.RdfClassAttribute";

        private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // The predicates deliberately do not filter on 'partial'. A non-partial declaration is the
            // single most common authoring mistake and produces no mapping at all, so it has to reach
            // the transform in order to be reported rather than silently dropped here.
            var properties = context.SyntaxProvider.ForAttributeWithMetadataName(
                    RdfPropertyAttribute,
                    predicate: static (node, _) => node is PropertyDeclarationSyntax,
                    transform: static (ctx, _) => PropertyInfo.From(ctx));

            var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
                    RdfClassAttribute,
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => ClassInfo.From(ctx));

            var combined = properties.Collect().Combine(classes.Collect());

            context.RegisterSourceOutput(combined, static (spc, data) => Emit(spc, data.Left, data.Right));
        }

        private static void Emit(SourceProductionContext context, ImmutableArray<PropertyResult> properties, ImmutableArray<ClassResult> classes)
        {
            var types = new Dictionary<string, TypeAggregate>();

            foreach (PropertyResult result in properties)
            {
                if (result.Diagnostic is not null)
                {
                    context.ReportDiagnostic(result.Diagnostic.ToDiagnostic());
                }

                if (result.Info is not PropertyInfo property)
                {
                    continue;
                }

                if (!types.TryGetValue(property.TypeKey, out TypeAggregate aggregate))
                {
                    aggregate = new TypeAggregate(property.Namespace, property.TypeName);
                    types.Add(property.TypeKey, aggregate);
                }

                aggregate.Properties.Add(property);
            }

            foreach (ClassResult result in classes)
            {
                if (result.Diagnostic is not null)
                {
                    context.ReportDiagnostic(result.Diagnostic.ToDiagnostic());
                }

                if (result.Info is not ClassInfo cls)
                {
                    continue;
                }

                if (!types.TryGetValue(cls.TypeKey, out TypeAggregate aggregate))
                {
                    aggregate = new TypeAggregate(cls.Namespace, cls.TypeName);
                    types.Add(cls.TypeKey, aggregate);
                }

                aggregate.ClassUris = cls.ClassUris;
            }

            foreach (var entry in types)
            {
                context.AddSource(FileName(entry.Key), SourceText.From(Render(entry.Value), Encoding.UTF8));
            }
        }

        private static string Render(TypeAggregate type)
        {
            var source = new StringBuilder();

            source.AppendLine("// <auto-generated/> Semiodesk.Trinity mapping generator");
            source.AppendLine("#nullable enable");
            source.AppendLine();

            bool hasNamespace = !string.IsNullOrEmpty(type.Namespace);

            if (hasNamespace)
            {
                source.Append("namespace ").AppendLine(type.Namespace);
                source.AppendLine("{");
            }

            source.Append("    partial class ").AppendLine(type.TypeName);
            source.AppendLine("    {");

            foreach (PropertyInfo p in type.Properties)
            {
                string field = p.PropertyName + "PropertyMapping";

                source.Append("        protected global::Semiodesk.Trinity.PropertyMapping<").Append(p.PropertyType).Append("> ")
                    .Append(field)
                    .Append(" = new global::Semiodesk.Trinity.PropertyMapping<").Append(p.PropertyType).Append(">(\"")
                    .Append(p.PropertyName).Append("\", \"").Append(p.Uri).Append("\"");

                if (p.CollectionConcreteType is not null)
                {
                    source.Append(", new ").Append(p.CollectionConcreteType).Append("()");
                }

                if (p.LanguageInvariant)
                {
                    source.Append(", true");
                }

                source.AppendLine(");");

                source.Append("        public partial ").Append(p.PropertyType).Append(' ').AppendLine(p.PropertyName);
                source.AppendLine("        {");
                source.Append("            get { return GetValue(").Append(field).AppendLine("); }");
                source.Append("            set { SetValue(").Append(field).AppendLine(", value); }");
                source.AppendLine("        }");
            }

            if (!string.IsNullOrEmpty(type.ClassUris))
            {
                source.AppendLine("        public override global::System.Collections.Generic.IEnumerable<global::Semiodesk.Trinity.Class> GetTypes()");
                source.AppendLine("        {");
                foreach (string classUri in type.ClassUris!.Split('\n'))
                {
                    source.Append("            yield return new global::Semiodesk.Trinity.Class(\"").Append(classUri).AppendLine("\");");
                }
                source.AppendLine("        }");
            }

            source.AppendLine("    }");

            if (hasNamespace)
            {
                source.AppendLine("}");
            }

            return source.ToString();
        }

        private static string? GetCollectionConcreteType(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named || !named.IsGenericType || named.TypeArguments.Length != 1)
            {
                return null;
            }

            string element = named.TypeArguments[0].ToDisplayString(TypeFormat);

            switch (named.OriginalDefinition.ToDisplayString())
            {
                case "System.Collections.Generic.List<T>":
                case "System.Collections.Generic.IList<T>":
                case "System.Collections.Generic.ICollection<T>":
                case "System.Collections.Generic.IEnumerable<T>":
                case "System.Collections.Generic.IReadOnlyList<T>":
                case "System.Collections.Generic.IReadOnlyCollection<T>":
                    return "global::System.Collections.Generic.List<" + element + ">";
                case "System.Collections.ObjectModel.ObservableCollection<T>":
                    return "global::System.Collections.ObjectModel.ObservableCollection<" + element + ">";
                case "System.Collections.ObjectModel.Collection<T>":
                    return "global::System.Collections.ObjectModel.Collection<" + element + ">";
                default:
                    return null;
            }
        }

        private static bool IsTopLevel(INamedTypeSymbol? type) => type is not null && type.ContainingType is null;

        private static bool IsPartial(SyntaxNode node) =>
            node is MemberDeclarationSyntax member &&
            member.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

        private static Location IdentifierLocation(SyntaxNode node)
        {
            switch (node)
            {
                case PropertyDeclarationSyntax property:
                    return property.Identifier.GetLocation();
                case ClassDeclarationSyntax cls:
                    return cls.Identifier.GetLocation();
                default:
                    return node.GetLocation();
            }
        }

        private static bool DerivesFromResource(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.ToDisplayString() == "Semiodesk.Trinity.Resource")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether Trinity can materialize the type, which it does with
        /// <c>Activator.CreateInstance(type, uri)</c> while marshalling query results.
        /// </summary>
        private static bool HasUriConstructor(INamedTypeSymbol type)
        {
            // Abstract types are never instantiated directly; the concrete subclass is what matters.
            if (type.IsAbstract)
            {
                return true;
            }

            foreach (IMethodSymbol constructor in type.InstanceConstructors)
            {
                if (constructor.DeclaredAccessibility == Accessibility.Private ||
                    constructor.Parameters.Length != 1)
                {
                    continue;
                }

                string parameter = constructor.Parameters[0].Type.ToDisplayString();

                if (parameter == "System.Uri" || parameter == "Semiodesk.Trinity.UriRef")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A diagnostic held in the incremental pipeline. <see cref="Diagnostic"/> itself is not a good
        /// cache key, so the pieces are carried in an equatable record and the diagnostic is built only
        /// when it is reported.
        /// </summary>
        private sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, Location Location, string Name)
        {
            public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location, Name);
        }

        /// <summary>The outcome of inspecting one <c>[RdfProperty]</c> declaration.</summary>
        private sealed record PropertyResult(PropertyInfo? Info, DiagnosticInfo? Diagnostic);

        /// <summary>The outcome of inspecting one <c>[RdfClass]</c> declaration.</summary>
        private sealed record ClassResult(ClassInfo? Info, DiagnosticInfo? Diagnostic);

        private static string GetNamespace(INamedTypeSymbol type) =>
            type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();

        private static string GetTypeName(INamedTypeSymbol type) =>
            type.IsGenericType
                ? type.Name + "<" + string.Join(", ", type.TypeParameters.Select(tp => tp.Name)) + ">"
                : type.Name;

        private static string FileName(string typeKey)
        {
            var builder = new StringBuilder(typeKey.Length);

            foreach (char c in typeKey)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return builder.Append(".Mapping.g.cs").ToString();
        }

        private sealed class TypeAggregate
        {
            public TypeAggregate(string ns, string typeName)
            {
                Namespace = ns;
                TypeName = typeName;
            }

            public string Namespace { get; }

            public string TypeName { get; }

            public string? ClassUris { get; set; }

            public List<PropertyInfo> Properties { get; } = new List<PropertyInfo>();
        }

        private sealed record PropertyInfo(
            string TypeKey,
            string Namespace,
            string TypeName,
            string PropertyName,
            string PropertyType,
            string Uri,
            bool LanguageInvariant,
            string? CollectionConcreteType)
        {
            public static PropertyResult From(GeneratorAttributeSyntaxContext ctx)
            {
                if (ctx.TargetSymbol is not IPropertySymbol prop)
                {
                    return new PropertyResult(null, null);
                }

                Location location = IdentifierLocation(ctx.TargetNode);

                if (!IsPartial(ctx.TargetNode))
                {
                    return new PropertyResult(null, new DiagnosticInfo(
                        MappingDiagnostics.PropertyMustBePartial, location, prop.Name));
                }

                if (!IsTopLevel(prop.ContainingType))
                {
                    return new PropertyResult(null, new DiagnosticInfo(
                        MappingDiagnostics.TypeMustBeTopLevel, location, prop.ContainingType.Name));
                }

                AttributeData attribute = ctx.Attributes[0];

                if (attribute.ConstructorArguments.Length == 0 ||
                    attribute.ConstructorArguments[0].Value is not string uri)
                {
                    return new PropertyResult(null, null);
                }

                bool languageInvariant =
                    attribute.ConstructorArguments.Length > 1 &&
                    attribute.ConstructorArguments[1].Value is bool b && b;

                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == "LanguageInvariant" && named.Value.Value is bool nb)
                    {
                        languageInvariant = nb;
                    }
                }

                INamedTypeSymbol type = prop.ContainingType;

                return new PropertyResult(new PropertyInfo(
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamespace(type),
                    GetTypeName(type),
                    prop.Name,
                    prop.Type.ToDisplayString(TypeFormat),
                    uri,
                    languageInvariant,
                    GetCollectionConcreteType(prop.Type)), null);
            }
        }

        private sealed record ClassInfo(
            string TypeKey,
            string Namespace,
            string TypeName,
            string ClassUris)
        {
            public static ClassResult From(GeneratorAttributeSyntaxContext ctx)
            {
                if (ctx.TargetSymbol is not INamedTypeSymbol type)
                {
                    return new ClassResult(null, null);
                }

                Location location = IdentifierLocation(ctx.TargetNode);

                // Reported one at a time, most fundamental first: each of these stops the mapping from
                // being generated, so there is no value in listing the consequences of the first.
                if (!IsPartial(ctx.TargetNode))
                {
                    return new ClassResult(null, new DiagnosticInfo(
                        MappingDiagnostics.ClassMustBePartial, location, type.Name));
                }

                if (!IsTopLevel(type))
                {
                    return new ClassResult(null, new DiagnosticInfo(
                        MappingDiagnostics.TypeMustBeTopLevel, location, type.Name));
                }

                if (!DerivesFromResource(type))
                {
                    return new ClassResult(null, new DiagnosticInfo(
                        MappingDiagnostics.ClassMustDeriveFromResource, location, type.Name));
                }

                string uris = string.Join("\n", ctx.Attributes
                    .Select(a => a.ConstructorArguments.Length > 0 ? a.ConstructorArguments[0].Value as string : null)
                    .Where(s => !string.IsNullOrEmpty(s)));

                if (uris.Length == 0)
                {
                    return new ClassResult(null, null);
                }

                // A missing constructor does not stop generation — the mapping is still correct, the type
                // just cannot be read back from a store — so the mapping is emitted alongside the warning.
                DiagnosticInfo? constructor = HasUriConstructor(type)
                    ? null
                    : new DiagnosticInfo(MappingDiagnostics.ClassNeedsUriConstructor, location, type.Name);

                return new ClassResult(new ClassInfo(
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamespace(type),
                    GetTypeName(type),
                    uris), constructor);
            }
        }
    }
}
