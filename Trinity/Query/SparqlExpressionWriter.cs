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
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF.Query.Expressions;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Serializes a SPARQL expression tree back to text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because dotNetRDF's own serialization is not faithful: it drops the parentheses
    /// around the operand of a negation, so <c>FILTER(!(?r &lt; 3))</c> comes back as
    /// <c>FILTER(!?r &lt; 3)</c> — that is <c>(!?r) &lt; 3</c>, a different question — and the result
    /// still parses, so nothing flags it. <c>SparqlFormatter</c> has the same defect. Parsing is fine;
    /// the tree is correct, and only writing it out is wrong.
    /// </para>
    /// <para>
    /// The fix is to parenthesise <b>every</b> operator application rather than emit the minimum a
    /// precedence table would allow. Redundant parentheses cannot change meaning, so correctness needs
    /// no precedence knowledge at all — which is the point, because a precedence table is exactly the
    /// kind of thing that is subtly wrong in one corner. Forms that already delimit themselves —
    /// function calls, aggregates — keep their own shape, with their arguments serialized recursively
    /// so a bad expression cannot hide inside one.
    /// </para>
    /// <para>
    /// Verified against a corpus of expressions covering negation over comparison, negation over
    /// conjunction and disjunction, nested negation, <c>IN</c>/<c>NOT IN</c>, <c>REGEX</c>, <c>IF</c>,
    /// <c>COALESCE</c>, unary minus and arithmetic precedence: faithful in every case, where
    /// dotNetRDF's serialization is faithful in 17 of 22 and produces text that does not even reparse
    /// for a doubly nested negation. See <c>SparqlExpressionWriterTest</c>.
    /// </para>
    /// </remarks>
    internal static class SparqlExpressionWriter
    {
        /// <summary>
        /// Writes an expression as SPARQL text.
        /// </summary>
        internal static string Write(ISparqlExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            ISparqlExpression[] arguments = Arguments(expression);

            // A leaf - a variable, an IRI, a literal. Its own ToString is authoritative and there is
            // nothing nested that could be mis-parenthesised.
            if (arguments.Length == 0)
            {
                return expression.ToString();
            }

            switch (expression.Type)
            {
                case SparqlExpressionType.UnaryOperator:
                    return "(" + expression.Functor + Write(arguments[0]) + ")";

                case SparqlExpressionType.BinaryOperator:
                    return "(" + Write(arguments[0]) + " " + expression.Functor + " " + Write(arguments[1]) + ")";

                case SparqlExpressionType.SetOperator:
                    // ?x IN (a, b, c) - the first argument is the tested value, the rest the set.
                    return "(" + Write(arguments[0]) + " " + expression.Functor + " (" +
                           string.Join(", ", arguments.Skip(1).Select(Write)) + "))";

                case SparqlExpressionType.Function:
                case SparqlExpressionType.Aggregate:
                    return FunctionName(expression.Functor) + "(" +
                           string.Join(", ", arguments.Select(Write)) + ")";

                default:
                    // Primary and anything a future version adds. Refusing here rather than guessing
                    // keeps an unrecognised form from being written out incorrectly.
                    throw new NotSupportedException(
                        $"The SPARQL expression '{expression}' is of kind {expression.Type}, which cannot be " +
                        "serialized faithfully. It is refused rather than written out approximately, because a " +
                        "silently altered expression would answer a different question than the one asked.");
            }
        }

        /// <summary>
        /// Renders an expression as a structural signature: operators and nesting, with leaves
        /// stringified. Two expressions with equal signatures have the same shape.
        /// </summary>
        /// <remarks>
        /// Used to confirm that a re-serialized query still means what it did, which is what turns
        /// dotNetRDF's serialization defect into a refusal rather than a wrong answer.
        /// </remarks>
        internal static string Signature(ISparqlExpression expression)
        {
            if (expression == null)
            {
                return "(null)";
            }

            ISparqlExpression[] arguments = Arguments(expression);

            if (arguments.Length == 0)
            {
                return expression.ToString();
            }

            return expression.Functor + "(" + string.Join(",", arguments.Select(Signature)) + ")";
        }

        /// <summary>
        /// A function's name is written bare when it is a keyword and bracketed when it is an IRI.
        /// </summary>
        private static string FunctionName(string functor)
        {
            if (string.IsNullOrEmpty(functor))
            {
                return functor;
            }

            bool isIri = functor.IndexOf(':') > 0 &&
                         (functor.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                          functor.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                          functor.StartsWith("urn:", StringComparison.OrdinalIgnoreCase));

            return isIri ? "<" + functor + ">" : functor;
        }

        private static ISparqlExpression[] Arguments(ISparqlExpression expression)
        {
            IEnumerable<ISparqlExpression> arguments = expression.Arguments;

            return arguments == null ? new ISparqlExpression[0] : arguments.ToArray();
        }
    }
}
