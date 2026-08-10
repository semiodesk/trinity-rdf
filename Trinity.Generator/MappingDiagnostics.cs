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

        /// <summary>TRIN005: a mapped class with no constructor Trinity can materialize.</summary>
        public static readonly DiagnosticDescriptor ClassNeedsUriConstructor = new DiagnosticDescriptor(
            id: "TRIN005",
            title: "Mapped class needs a Uri constructor",
            messageFormat: "Class '{0}' has no accessible constructor taking a single Uri, so Trinity cannot materialize it when reading from a store",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Resources are constructed by Activator.CreateInstance(type, uri) while marshalling query results. Without such a constructor the failure appears at runtime, not at build time.");
    }
}
