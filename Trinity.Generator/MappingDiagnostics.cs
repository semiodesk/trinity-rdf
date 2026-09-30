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

using Microsoft.CodeAnalysis;

namespace Semiodesk.Trinity.Generator
{
    /// <summary>
    /// Diagnostics reported by <see cref="MappingSourceGenerator"/>.
    /// </summary>
    /// <remarks>
    /// Every one of these describes a case where an <c>[RdfClass]</c> or <c>[RdfProperty]</c> attribute
    /// looks correct, compiles, and then does nothing — the generator simply skips the declaration. Those
    /// are the failures worth a diagnostic: they are invisible until a query silently returns nothing at
    /// runtime, which is exactly the point at which they are most expensive to diagnose.
    ///
    /// All are warnings rather than errors. The authoring model changed in 2.0 (mapped members must be
    /// <c>partial</c>), so a class carried over from 1.x would otherwise fail to build instead of
    /// reporting what to fix.
    ///
    /// TRIN007 is the one exception to the "generator skips the declaration" description above: the
    /// mapping <i>is</i> generated, it just uses a type whose equality is wrong for RDF. It is reported
    /// so the fix appears at build time, next to the declaration; <c>PropertyMapping&lt;T&gt;</c> refuses
    /// the same type at runtime, which is what covers mappings written by hand (ADR-0018) that this
    /// generator never sees.
    /// </remarks>
    internal static class MappingDiagnostics
    {
        private const string Category = "Trinity.Mapping";

        /// <summary>TRIN001: <c>[RdfProperty]</c> on a property that is not <c>partial</c>.</summary>
        public static readonly DiagnosticDescriptor PropertyMustBePartial = new DiagnosticDescriptor(
            id: "TRIN001",
            title: "Mapped property must be partial",
            messageFormat: "Property '{0}' is marked [RdfProperty] but is not declared 'partial', so no mapping is generated and the attribute has no effect",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The generator supplies the implementing half of a partial property. A property that is not partial keeps its own accessors, which never reach the RDF mapping.");

        /// <summary>TRIN002: <c>[RdfClass]</c> on a class that is not <c>partial</c>.</summary>
        public static readonly DiagnosticDescriptor ClassMustBePartial = new DiagnosticDescriptor(
            id: "TRIN002",
            title: "Mapped class must be partial",
            messageFormat: "Class '{0}' is marked [RdfClass] but is not declared 'partial', so no GetTypes() override is generated and the attribute has no effect",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Without a generated GetTypes() override the class has no RDF type, so its resources are written untyped and queries for the class return nothing.");

        /// <summary>TRIN003: the mapped type is nested inside another type.</summary>
        public static readonly DiagnosticDescriptor TypeMustBeTopLevel = new DiagnosticDescriptor(
            id: "TRIN003",
            title: "Mapped type must be top-level",
            messageFormat: "'{0}' is a nested type; Trinity mapping is only generated for top-level types, so the attribute has no effect",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The generator emits a partial declaration in the containing namespace and cannot reopen a nested type. Move the mapped type to the namespace level.");

        /// <summary>TRIN004: a mapped class that does not derive from <c>Resource</c>.</summary>
        public static readonly DiagnosticDescriptor ClassMustDeriveFromResource = new DiagnosticDescriptor(
            id: "TRIN004",
            title: "Mapped class must derive from Resource",
            messageFormat: "Class '{0}' is marked [RdfClass] but does not derive from Semiodesk.Trinity.Resource; the generated mapping members will not compile",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The generated accessors call GetValue/SetValue and override GetTypes(), all of which are declared by Resource.");

        /// <summary>TRIN006: a mapped URI that is not a term of the vocabulary it belongs to.</summary>
        public static readonly DiagnosticDescriptor UnknownVocabularyTerm = new DiagnosticDescriptor(
            id: "TRIN006",
            title: "URI is not a term in the vocabulary",
            messageFormat: "'{0}' is not a term in the generated '{1}' vocabulary — check for a typo",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A mistyped URI compiles and matches nothing at runtime, so a query simply returns no results. Only vocabularies generated by trinity-vocab are checked, since only those are known to list every term.");

        /// <summary>TRIN007: a mapped property whose value type is <c>System.Uri</c>.</summary>
        public static readonly DiagnosticDescriptor MappedTypeMustNotBeRawUri = new DiagnosticDescriptor(
            id: "TRIN007",
            title: "Mapped property must use UriRef instead of Uri",
            messageFormat: "Property '{0}' is mapped to a System.Uri; use Semiodesk.Trinity.UriRef instead, because System.Uri equality ignores the fragment",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "System.Uri.Equals ignores the fragment (RFC 3986), so http://example.org/x#a and http://example.org/x#b compare equal - in RDF they are two different resources. On .NET 10, where System.Uri implements IEquatable<Uri>, that also applies inside every generic collection. UriRef derives from Uri and compares fragments, so changing the declared type is the whole fix. PropertyMapping<T> rejects System.Uri at runtime.");

        /// <summary>TRIN005: a mapped class with no constructor Trinity can materialize.</summary>
        public static readonly DiagnosticDescriptor ClassNeedsUriConstructor = new DiagnosticDescriptor(
            id: "TRIN005",
            title: "Mapped class needs a Uri constructor",
            messageFormat: "Class '{0}' has no accessible constructor taking a single Uri, so Trinity cannot materialize it when reading from a store",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Resources are constructed by Activator.CreateInstance(type, uri) while marshalling query results. Without such a constructor the failure appears at runtime, not at build time.");

        /// <summary>TRIN009: a localized-text container declared with a setter.</summary>
        public static readonly DiagnosticDescriptor ContainerMustBeGetOnly = new DiagnosticDescriptor(
            id: "TRIN009",
            title: "Localized container property should be get-only",
            messageFormat: "Property '{0}' is a localized-text container and declares a setter; declare it get-only, because the container is mutated in place",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A LocalizedString or LocalizedStringCollection is a mutable view owned by the mapping, not a value to assign. A setter admits two hazards the get-only form removes at compile time: assigning null, after which every use is a NullReferenceException, and assigning another resource's container, which aliases one instance across both so a write through either is visible through the other. Write label[\"de\"] = \"Hallo\" instead of label = something (ADR-0048).");

        /// <summary>TRIN010: a localized-text container type the mapping engine cannot use.</summary>
        public static readonly DiagnosticDescriptor UnsupportedLocalizedContainer = new DiagnosticDescriptor(
            id: "TRIN010",
            title: "Localized container type is not supported",
            messageFormat: "Property '{0}' is typed '{1}'; only LocalizedString and LocalizedStringCollection can be mapped",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "ILocalizedText is the shared surface of the two containers, not an extension point: the mapping engine dispatches on their concrete types to add a value and to copy one container into another. PropertyMapping refuses anything else at registration, so without this the failure arrives at runtime, from a field initializer, on the first construction of the class - and for a property declared as the interface itself it is a MissingMethodException from Activator, which names neither the property nor the cause (ADR-0048).");

        /// <summary>TRIN008: <c>[RdfProperty(uri, languageInvariant)]</c>, which no longer does anything.</summary>
        public static readonly DiagnosticDescriptor LanguageInvariantIsObsolete = new DiagnosticDescriptor(
            id: "TRIN008",
            title: "languageInvariant no longer has an effect",
            messageFormat: "Property '{0}' passes languageInvariant to [RdfProperty]; the flag is ignored, because the property's declared type now decides how language tags are handled",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A string property is language-invariant by construction: it sees untagged literals only. Declare LocalizedString (one value per language) or LocalizedStringCollection (several) for language-tagged values, or LangString for the raw tagged literal. Removing the flag is usually the whole migration: in 1.x a string property only ever showed tagged values while Resource.Language was set, so code that never set it was already reading untagged literals and keeps behaving identically as a plain string. Change the declared type only where the old code did set Resource.Language, since that is the only case whose values were language-tagged. Reported rather than silently ignored because a flag that no longer does anything is otherwise indistinguishable from one that does (ADR-0048).");
    }
}
