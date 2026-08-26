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

using System;
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

                if (result.MappedTypeIsRawUri is not null)
                {
                    context.ReportDiagnostic(result.MappedTypeIsRawUri.ToDiagnostic());
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

            // Classes carrying [RdfClass] are the authority on their own partial-ness, so they are
            // reported first and their locations remembered.
            var reportedClasses = new HashSet<Location>();

            foreach (ClassResult result in classes)
            {
                foreach (DiagnosticInfo diagnostic in result.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());

                    if (diagnostic.Descriptor.Equals(MappingDiagnostics.ClassMustBePartial))
                    {
                        reportedClasses.Add(diagnostic.Location);
                    }
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

            // A class with mapped properties but no [RdfClass] of its own would otherwise only produce
            // per-property warnings and never be named. Deduplicated against the loop above.
            foreach (PropertyResult result in properties)
            {
                DiagnosticInfo? containing = result.ContainingClassNotPartial;

                if (containing is not null && reportedClasses.Add(containing.Location))
                {
                    context.ReportDiagnostic(containing.ToDiagnostic());
                }
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

                source.Append("        ").Append(p.Modifiers).Append(' ').Append(p.PropertyType)
                    .Append(' ').AppendLine(p.PropertyName);
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
            string? container = GetCollectionContainer(type);

            if (container is null)
            {
                return null;
            }

            string element = ((INamedTypeSymbol)type).TypeArguments[0].ToDisplayString(TypeFormat);

            return container + "<" + element + ">";
        }

        /// <summary>
        /// The concrete collection type to instantiate for a mapped collection property, without its
        /// type argument, or <c>null</c> if the type is not a recognized collection.
        /// </summary>
        private static string? GetCollectionContainer(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named || !named.IsGenericType || named.TypeArguments.Length != 1)
            {
                return null;
            }

            switch (named.OriginalDefinition.ToDisplayString())
            {
                case "System.Collections.Generic.List<T>":
                case "System.Collections.Generic.IList<T>":
                case "System.Collections.Generic.ICollection<T>":
                case "System.Collections.Generic.IEnumerable<T>":
                case "System.Collections.Generic.IReadOnlyList<T>":
                case "System.Collections.Generic.IReadOnlyCollection<T>":
                    return "global::System.Collections.Generic.List";
                case "System.Collections.ObjectModel.ObservableCollection<T>":
                    return "global::System.Collections.ObjectModel.ObservableCollection";
                case "System.Collections.ObjectModel.Collection<T>":
                    return "global::System.Collections.ObjectModel.Collection";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Indicates whether a mapped property stores <c>System.Uri</c> values, either directly or as
        /// the element type of a mapped collection.
        /// </summary>
        /// <remarks>
        /// Matched on exact identity rather than derivation, which is the point: <see cref="Uri"/> is
        /// wrong for RDF identity and <c>UriRef</c>, which derives from it, is the fix. A check phrased
        /// as "assignable to Uri" would flag the fix as well as the defect.
        /// </remarks>
        private static bool IsRawUri(ITypeSymbol type)
        {
            ITypeSymbol candidate = GetCollectionContainer(type) is null
                ? type
                : ((INamedTypeSymbol)type).TypeArguments[0];

            return candidate.Name == "Uri"
                && candidate.ContainingNamespace?.ToDisplayString() == "System";
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
        /// <remarks>
        /// <paramref name="ContainingClassNotPartial"/> is carried here so a class that has mapped
        /// properties but no <c>[RdfClass]</c> is still told it needs to be partial. Without it, such a
        /// class produces only per-property warnings and never names the class itself.
        /// </remarks>
        /// <remarks>
        /// <paramref name="MappedTypeIsRawUri"/> is separate from <paramref name="Diagnostic"/> because
        /// it is the only property-level diagnostic that does not suppress the mapping: the generated
        /// code is fine, the declared type is not. Reporting it through <paramref name="Diagnostic"/>
        /// would mean returning early and dropping a mapping that ought to be emitted.
        /// </remarks>
        private sealed record PropertyResult(
            PropertyInfo? Info,
            DiagnosticInfo? Diagnostic,
            DiagnosticInfo? ContainingClassNotPartial,
            DiagnosticInfo? MappedTypeIsRawUri = null);

        /// <summary>The outcome of inspecting one <c>[RdfClass]</c> declaration.</summary>
        /// <remarks>
        /// Holds every applicable diagnostic rather than the first one. The checks are independent — a
        /// class can be non-partial *and* lack a Uri constructor — and someone migrating a large model
        /// wants the whole list in one build, not one item per build. Equality is structural so the
        /// incremental pipeline still caches.
        /// </remarks>
        private sealed class ClassResult : IEquatable<ClassResult>
        {
            public ClassResult(ClassInfo? info, ImmutableArray<DiagnosticInfo> diagnostics)
            {
                Info = info;
                Diagnostics = diagnostics;
            }

            public ClassInfo? Info { get; }

            public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

            public bool Equals(ClassResult? other) =>
                other is not null &&
                Equals(Info, other.Info) &&
                Diagnostics.SequenceEqual(other.Diagnostics);

            public override bool Equals(object? other) => Equals(other as ClassResult);

            public override int GetHashCode()
            {
                int hash = Info?.GetHashCode() ?? 0;

                foreach (DiagnosticInfo diagnostic in Diagnostics)
                {
                    hash = (hash * 397) ^ diagnostic.GetHashCode();
                }

                return hash;
            }
        }

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

        /// <summary>
        /// One mapped property, as the implementing half needs to be written.
        /// </summary>
        /// <param name="Modifiers">
        /// The declaring declaration's modifiers, verbatim. C# requires both halves of a partial property
        /// to agree on accessibility and on the <c>virtual</c>/<c>override</c>/<c>sealed</c>/<c>new</c>
        /// combination, so these are copied rather than assumed — hardcoding <c>public partial</c> made
        /// <c>public new partial</c> an unfixable CS8800, and any non-public mapped property a CS8799.
        /// Copying the list verbatim also preserves the author's ordering, which C# allows to vary.
        /// </param>
        private sealed record PropertyInfo(
            string TypeKey,
            string Namespace,
            string TypeName,
            string PropertyName,
            string PropertyType,
            string Uri,
            bool LanguageInvariant,
            string? CollectionConcreteType,
            string Modifiers)
        {
            public static PropertyResult From(GeneratorAttributeSyntaxContext ctx)
            {
                if (ctx.TargetSymbol is not IPropertySymbol prop)
                {
                    return new PropertyResult(null, null, null);
                }

                Location location = IdentifierLocation(ctx.TargetNode);

                // Verbatim, so accessibility and the new/virtual/override/sealed combination match the
                // declaring half exactly. 'partial' is already among them, since only partial members
                // reach emission.
                string modifiers = ctx.TargetNode is PropertyDeclarationSyntax declaration
                    ? string.Join(" ", declaration.Modifiers.Select(m => m.Text))
                    : "public partial";

                // Reported even when the class carries no [RdfClass] of its own, so a class with only
                // mapped properties is still told it must be partial. Deduplicated in Emit against the
                // [RdfClass] pipeline, which reports the same thing for classes that have one.
                DiagnosticInfo? containingClassNotPartial = null;
                ClassDeclarationSyntax? containingClass =
                    ctx.TargetNode.FirstAncestorOrSelf<ClassDeclarationSyntax>();

                if (containingClass is not null &&
                    !containingClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    containingClassNotPartial = new DiagnosticInfo(
                        MappingDiagnostics.ClassMustBePartial,
                        containingClass.Identifier.GetLocation(),
                        containingClass.Identifier.ValueText);
                }

                if (!IsPartial(ctx.TargetNode))
                {
                    return new PropertyResult(null, new DiagnosticInfo(
                        MappingDiagnostics.PropertyMustBePartial, location, prop.Name),
                        containingClassNotPartial);
                }

                if (!IsTopLevel(prop.ContainingType))
                {
                    return new PropertyResult(null, new DiagnosticInfo(
                        MappingDiagnostics.TypeMustBeTopLevel, location, prop.ContainingType.Name),
                        containingClassNotPartial);
                }

                AttributeData attribute = ctx.Attributes[0];

                if (attribute.ConstructorArguments.Length == 0 ||
                    attribute.ConstructorArguments[0].Value is not string uri)
                {
                    return new PropertyResult(null, null, containingClassNotPartial);
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

                DiagnosticInfo? mappedTypeIsRawUri = IsRawUri(prop.Type)
                    ? new DiagnosticInfo(MappingDiagnostics.MappedTypeMustNotBeRawUri, location, prop.Name)
                    : null;

                return new PropertyResult(new PropertyInfo(
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamespace(type),
                    GetTypeName(type),
                    prop.Name,
                    prop.Type.ToDisplayString(TypeFormat),
                    uri,
                    languageInvariant,
                    GetCollectionConcreteType(prop.Type),
                    modifiers), null, containingClassNotPartial, mappedTypeIsRawUri);
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
                    return new ClassResult(null, ImmutableArray<DiagnosticInfo>.Empty);
                }

                Location location = IdentifierLocation(ctx.TargetNode);
                var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

                // Every applicable check is reported, not just the first. They are independent — a class
                // can be non-partial *and* lack a Uri constructor — and someone migrating a large model
                // wants the full list from one build rather than one problem per build.
                bool partial = IsPartial(ctx.TargetNode);
                bool topLevel = IsTopLevel(type);
                bool resource = DerivesFromResource(type);

                if (!partial)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        MappingDiagnostics.ClassMustBePartial, location, type.Name));
                }

                if (!topLevel)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        MappingDiagnostics.TypeMustBeTopLevel, location, type.Name));
                }

                if (!resource)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        MappingDiagnostics.ClassMustDeriveFromResource, location, type.Name));
                }

                if (!HasUriConstructor(type))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        MappingDiagnostics.ClassNeedsUriConstructor, location, type.Name));
                }

                string uris = string.Join("\n", ctx.Attributes
                    .Select(a => a.ConstructorArguments.Length > 0 ? a.ConstructorArguments[0].Value as string : null)
                    .Where(s => !string.IsNullOrEmpty(s)));

                // Emission still requires every structural condition. A missing Uri constructor does not
                // stop it: the mapping itself is correct, only materialization from a store would fail.
                ClassInfo? info = partial && topLevel && resource && uris.Length > 0
                    ? new ClassInfo(
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        GetNamespace(type),
                        GetTypeName(type),
                        uris)
                    : null;

                return new ClassResult(info, diagnostics.ToImmutable());
            }
        }
    }
}
