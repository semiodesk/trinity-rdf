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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Semiodesk.Trinity.Generator
{
    /// <summary>
    /// Reports <c>[RdfClass]</c> and <c>[RdfProperty]</c> URIs that belong to a known vocabulary but are
    /// not one of its terms — in practice, typos.
    /// </summary>
    /// <remarks>
    /// A mistyped URI is invisible: it compiles, the mapping is generated, and the property simply never
    /// matches anything in the store, so a query returns nothing and looks like a data problem.
    ///
    /// The rule is deliberately narrow. It only considers vocabularies marked
    /// <c>[GeneratedCode("trinity-vocab", …)]</c>, because only a generated vocabulary is known to list
    /// every term. A hand-written vocabulary class is usually partial, so validating against one would
    /// flag correct URIs — and a rule that cries wolf gets suppressed along with TRIN001–005. A URI whose
    /// namespace matches no generated vocabulary is therefore never reported.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class VocabularyUriAnalyzer : DiagnosticAnalyzer
    {
        private const string RdfPropertyAttribute = "Semiodesk.Trinity.RdfPropertyAttribute";
        private const string RdfClassAttribute = "Semiodesk.Trinity.RdfClassAttribute";
        private const string GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";
        private const string GeneratorName = "trinity-vocab";

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(MappingDiagnostics.UnknownVocabularyTerm);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            // Mapped classes are ordinary source; the vocabularies they are checked against are the
            // generated part, and those are read from symbols rather than analyzed.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(start =>
            {
                // Built once per compilation. If the project has no generated vocabularies there is
                // nothing to validate against, so no per-symbol work is registered at all.
                IReadOnlyList<Vocabulary> vocabularies = FindVocabularies(start.Compilation);

                if (vocabularies.Count == 0)
                {
                    return;
                }

                start.RegisterSymbolAction(ctx => Analyze(ctx, vocabularies),
                    SymbolKind.Property, SymbolKind.NamedType);
            });
        }

        private static void Analyze(SymbolAnalysisContext context, IReadOnlyList<Vocabulary> vocabularies)
        {
            foreach (AttributeData attribute in context.Symbol.GetAttributes())
            {
                string? name = attribute.AttributeClass?.ToDisplayString();

                if (name != RdfPropertyAttribute && name != RdfClassAttribute)
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length == 0 ||
                    !(attribute.ConstructorArguments[0].Value is string uri) ||
                    string.IsNullOrEmpty(uri))
                {
                    continue;
                }

                // The longest matching namespace wins, so a vocabulary nested under another one's
                // namespace is still judged against itself.
                Vocabulary? owner = vocabularies
                    .Where(v => uri.StartsWith(v.Namespace, System.StringComparison.Ordinal))
                    .OrderByDescending(v => v.Namespace.Length)
                    .FirstOrDefault();

                // No generated vocabulary covers this URI, so nothing is known about it. Stay quiet.
                if (owner == null || owner.Terms.Contains(uri))
                {
                    continue;
                }

                Location? location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                                    ?? context.Symbol.Locations.FirstOrDefault();

                if (location != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MappingDiagnostics.UnknownVocabularyTerm, location, uri, owner.Prefix));
                }
            }
        }

        /// <summary>
        /// Collects the vocabularies this compilation can be checked against.
        /// </summary>
        /// <remarks>
        /// Only the compilation's own assembly is scanned. A vocabulary living in a referenced assembly is
        /// invisible here, which means URIs in its namespace go unchecked rather than wrongly flagged —
        /// the safe direction.
        /// </remarks>
        private static IReadOnlyList<Vocabulary> FindVocabularies(Compilation compilation)
        {
            var result = new List<Vocabulary>();

            Collect(compilation.Assembly.GlobalNamespace, result);

            return result;
        }

        private static void Collect(INamespaceSymbol ns, List<Vocabulary> result)
        {
            foreach (INamedTypeSymbol type in ns.GetTypeMembers())
            {
                Vocabulary? vocabulary = Read(type);

                if (vocabulary != null)
                {
                    result.Add(vocabulary);
                }
            }

            foreach (INamespaceSymbol nested in ns.GetNamespaceMembers())
            {
                Collect(nested, result);
            }
        }

        /// <summary>
        /// Reads a type as a vocabulary, or returns null if it is not one this analyzer trusts.
        /// </summary>
        private static Vocabulary? Read(INamedTypeSymbol type)
        {
            if (!IsGeneratedByTool(type))
            {
                return null;
            }

            string? ns = null;
            string? prefix = null;
            var terms = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
            {
                // Only the const-string companion carries readable values; the typed class initializes
                // its members with `new Property(new Uri(...))`, which is not a constant.
                if (!field.HasConstantValue || !(field.ConstantValue is string value))
                {
                    continue;
                }

                switch (field.Name)
                {
                    case "Namespace":
                        ns = value;
                        break;
                    case "Prefix":
                        prefix = value;
                        break;
                    default:
                        terms.Add(value);
                        break;
                }
            }

            // A vocabulary with no namespace or no terms tells us nothing, so it is not trusted.
            if (string.IsNullOrEmpty(ns) || terms.Count == 0)
            {
                return null;
            }

            // string.IsNullOrEmpty is not null-annotated on netstandard2.0, hence the assertions.
            return new Vocabulary(ns!, string.IsNullOrEmpty(prefix) ? type.Name : prefix!, terms);
        }

        private static bool IsGeneratedByTool(INamedTypeSymbol type) =>
            type.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == GeneratedCodeAttribute &&
                a.ConstructorArguments.Length > 0 &&
                a.ConstructorArguments[0].Value as string == GeneratorName);

        private sealed class Vocabulary
        {
            public Vocabulary(string ns, string prefix, HashSet<string> terms)
            {
                Namespace = ns;
                Prefix = prefix;
                Terms = terms;
            }

            public string Namespace { get; }

            public string Prefix { get; }

            public HashSet<string> Terms { get; }
        }
    }
}
