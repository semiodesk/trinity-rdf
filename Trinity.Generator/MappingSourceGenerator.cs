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
            var properties = context.SyntaxProvider.ForAttributeWithMetadataName(
                    RdfPropertyAttribute,
                    predicate: static (node, _) =>
                        node is PropertyDeclarationSyntax p &&
                        p.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                    transform: static (ctx, _) => PropertyInfo.From(ctx))
                .Where(static m => m is not null)
                .Select(static (m, _) => m!);

            var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
                    RdfClassAttribute,
                    predicate: static (node, _) =>
                        node is ClassDeclarationSyntax c &&
                        c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                    transform: static (ctx, _) => ClassInfo.From(ctx))
                .Where(static c => c is not null)
                .Select(static (c, _) => c!);

            var combined = properties.Collect().Combine(classes.Collect());

            context.RegisterSourceOutput(combined, static (spc, data) => Emit(spc, data.Left, data.Right));
        }

        private static void Emit(SourceProductionContext context, ImmutableArray<PropertyInfo> properties, ImmutableArray<ClassInfo> classes)
        {
            var types = new Dictionary<string, TypeAggregate>();

            foreach (PropertyInfo property in properties)
            {
                if (!types.TryGetValue(property.TypeKey, out TypeAggregate aggregate))
                {
                    aggregate = new TypeAggregate(property.Namespace, property.TypeName);
                    types.Add(property.TypeKey, aggregate);
                }

                aggregate.Properties.Add(property);
            }

            foreach (ClassInfo cls in classes)
            {
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
            public static PropertyInfo? From(GeneratorAttributeSyntaxContext ctx)
            {
                if (ctx.TargetSymbol is not IPropertySymbol prop || !IsTopLevel(prop.ContainingType))
                {
                    return null;
                }

                AttributeData attribute = ctx.Attributes[0];

                if (attribute.ConstructorArguments.Length == 0 ||
                    attribute.ConstructorArguments[0].Value is not string uri)
                {
                    return null;
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

                return new PropertyInfo(
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamespace(type),
                    GetTypeName(type),
                    prop.Name,
                    prop.Type.ToDisplayString(TypeFormat),
                    uri,
                    languageInvariant,
                    GetCollectionConcreteType(prop.Type));
            }
        }

        private sealed record ClassInfo(
            string TypeKey,
            string Namespace,
            string TypeName,
            string ClassUris)
        {
            public static ClassInfo? From(GeneratorAttributeSyntaxContext ctx)
            {
                if (ctx.TargetSymbol is not INamedTypeSymbol type || !IsTopLevel(type))
                {
                    return null;
                }

                string uris = string.Join("\n", ctx.Attributes
                    .Select(a => a.ConstructorArguments.Length > 0 ? a.ConstructorArguments[0].Value as string : null)
                    .Where(s => !string.IsNullOrEmpty(s)));

                if (uris.Length == 0)
                {
                    return null;
                }

                return new ClassInfo(
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamespace(type),
                    GetTypeName(type),
                    uris);
            }
        }
    }
}
