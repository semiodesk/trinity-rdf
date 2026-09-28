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
//
// Copyright (c) Semiodesk GmbH 2026

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Semiodesk.Trinity;
using System;
using System.Linq;

namespace Trinity.Generator.Tests
{
    /// <summary>
    /// Guards the decision that <see cref="LangString"/> has no conversion to or from
    /// <see cref="string"/> (ADR-0048), by asserting that the hazardous expressions do not compile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cannot be a normal unit test: there is nothing to call. The guarantee is the *absence* of a
    /// conversion, and the only way to assert an absence in C# is to compile the expression and require
    /// the error.
    /// </para>
    /// <para>
    /// It matters because adding <c>implicit operator string</c> looks like a convenience and is in fact
    /// the ADR-0025 hazard rebuilt: <c>label == "Hallo"</c> would bind to <c>string.operator ==</c>,
    /// compare lexical forms only, and return <c>true</c> for <c>"Hallo"@fr</c> — silently, in the most
    /// commonly written expression in the API. A future PR adding one turns this test red rather than
    /// shipping that.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class LangStringConversionTest
    {
        /// <summary>
        /// The expression this whole decision exists to break.
        /// </summary>
        [Test]
        public void ComparingALangStringToAStringDoesNotCompile()
        {
            Assert.AreEqual("CS0019", ErrorIn("bool M(LangString s) => s == \"Hallo\";"),
                "An implicit conversion to string would make this compile and silently ignore the tag.");
        }

        [Test]
        public void AStringDoesNotConvertToALangString()
        {
            Assert.AreEqual("CS0029", ErrorIn("LangString M() => \"Hallo\";"));
        }

        [Test]
        public void ALangStringDoesNotConvertToAString()
        {
            Assert.AreEqual("CS0029", ErrorIn("string M(LangString s) => s;"));
        }

        /// <summary>
        /// The counterpart: comparing two <c>LangString</c>s must compile, or the type would be unusable.
        /// Without this the test above would also pass if the operators were removed altogether.
        /// </summary>
        [Test]
        public void ComparingTwoLangStringsCompiles()
        {
            Assert.IsNull(ErrorIn("bool M(LangString a, LangString b) => a == b;"),
                "LangString must declare ==/!= or equal literals compare by reference (ADR-0025).");
        }

        [Test]
        public void ComparingALangStringToNullCompiles()
        {
            Assert.IsNull(ErrorIn("bool M(LangString s) => s == null;"));
        }

        /// <summary>
        /// Compiles <paramref name="member"/> inside a class and returns the id of the first error, or
        /// <c>null</c> when it compiles cleanly.
        /// </summary>
        private static string ErrorIn(string member)
        {
            var source = "using Semiodesk.Trinity;\nnamespace P { public class C { " + member + " } }";

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
                .Append(MetadataReference.CreateFromFile(typeof(LangString).Assembly.Location))
                .Distinct();

            var compilation = CSharpCompilation.Create(
                "LangStringConversionTest",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.LessOrEqual(errors.Count, 1,
                "Expected at most one error but got: " + string.Join(", ", errors.Select(d => d.ToString())));

            return errors.Count == 0 ? null : errors[0].Id;
        }
    }
}
