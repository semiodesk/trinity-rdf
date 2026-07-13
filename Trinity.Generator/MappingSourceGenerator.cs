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
    /// Emits the RDF mapping members that were historically produced by the cilg IL weaver:
    /// for each <c>partial</c> property annotated with <c>[RdfProperty]</c> a backing
    /// <c>PropertyMapping&lt;T&gt;</c> field plus the implementing getter/setter, and a
    /// <c>GetTypes()</c> override from the type's <c>[RdfClass]</c> attributes.
    ///
    /// Only <c>partial</c> mapped properties are handled, so this coexists with the legacy
    /// weaver (which handles plain auto-properties) during migration.
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
                    transform: static (ctx, _) => FromProperty(ctx))
                .Where(static m => m is not null)
                .Select(static (m, _) => m!);

            context.RegisterSourceOutput(properties.Collect(), static (spc, models) => Emit(spc, models));
        }

        private static Model? FromProperty(GeneratorAttributeSyntaxContext ctx)
        {
            if (ctx.TargetSymbol is not IPropertySymbol prop)
            {
                return null;
            }

            INamedTypeSymbol type = prop.ContainingType;

            // Top-level types only for now (mapped resource classes are not nested).
            if (type is null || type.ContainingType is not null)
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

            string classUris = string.Join("\n", type.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == RdfClassAttribute)
                .Select(a => a.ConstructorArguments.Length > 0 ? a.ConstructorArguments[0].Value as string : null)
                .Where(s => !string.IsNullOrEmpty(s)));

            string ns = type.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : type.ContainingNamespace.ToDisplayString();

            string typeName = type.IsGenericType
                ? type.Name + "<" + string.Join(", ", type.TypeParameters.Select(tp => tp.Name)) + ">"
                : type.Name;

            return new Model(
                TypeKey: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Namespace: ns,
                TypeName: typeName,
                PropertyName: prop.Name,
                PropertyType: prop.Type.ToDisplayString(TypeFormat),
                Uri: uri,
                LanguageInvariant: languageInvariant,
                CollectionConcreteType: GetCollectionConcreteType(prop.Type),
                ClassUris: classUris);
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

        private static void Emit(SourceProductionContext context, ImmutableArray<Model> models)
        {
            foreach (var group in models.GroupBy(m => m.TypeKey))
            {
                Model first = group.First();
                var source = new StringBuilder();

                source.AppendLine("// <auto-generated/> Semiodesk.Trinity mapping generator");
                source.AppendLine("#nullable enable");
                source.AppendLine();

                bool hasNamespace = !string.IsNullOrEmpty(first.Namespace);

                if (hasNamespace)
                {
                    source.Append("namespace ").AppendLine(first.Namespace);
                    source.AppendLine("{");
                }

                source.Append("    partial class ").AppendLine(first.TypeName);
                source.AppendLine("    {");

                foreach (Model m in group)
                {
                    string field = m.PropertyName + "PropertyMapping";

                    source.Append("        protected global::Semiodesk.Trinity.PropertyMapping<").Append(m.PropertyType).Append("> ")
                        .Append(field)
                        .Append(" = new global::Semiodesk.Trinity.PropertyMapping<").Append(m.PropertyType).Append(">(\"")
                        .Append(m.PropertyName).Append("\", \"").Append(m.Uri).Append("\"");

                    if (m.CollectionConcreteType is not null)
                    {
                        source.Append(", new ").Append(m.CollectionConcreteType).Append("()");
                    }

                    if (m.LanguageInvariant)
                    {
                        source.Append(", true");
                    }

                    source.AppendLine(");");

                    source.Append("        public partial ").Append(m.PropertyType).Append(' ').AppendLine(m.PropertyName);
                    source.AppendLine("        {");
                    source.Append("            get { return GetValue(").Append(field).AppendLine("); }");
                    source.Append("            set { SetValue(").Append(field).AppendLine(", value); }");
                    source.AppendLine("        }");
                }

                if (!string.IsNullOrEmpty(first.ClassUris))
                {
                    source.AppendLine("        public override global::System.Collections.Generic.IEnumerable<global::Semiodesk.Trinity.Class> GetTypes()");
                    source.AppendLine("        {");
                    foreach (string classUri in first.ClassUris.Split('\n'))
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

                context.AddSource(FileName(first.TypeKey), SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }

        private static string FileName(string typeKey)
        {
            var builder = new StringBuilder(typeKey.Length);

            foreach (char c in typeKey)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return builder.Append(".Mapping.g.cs").ToString();
        }

        private sealed record Model(
            string TypeKey,
            string Namespace,
            string TypeName,
            string PropertyName,
            string PropertyType,
            string Uri,
            bool LanguageInvariant,
            string? CollectionConcreteType,
            string ClassUris);
    }
}
