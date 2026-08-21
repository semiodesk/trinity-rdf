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
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Builds the SPARQL fragments that give an <see cref="ILayeredModel"/> its
    /// <c>(baseline − removals) ∪ additions</c> semantics. Single source of truth: every read
    /// path that honours the overlay composes its query from these, so the semantics cannot
    /// drift between the hand-written templates and the LINQ writer.
    /// </summary>
    /// <remarks>
    /// Subtraction cannot be expressed in a dataset clause — <c>FROM</c> is set union and SPARQL
    /// has no inverse — so it has to sit in the graph pattern. Every triple pattern is therefore
    /// replaced by <see cref="Overlay"/>, which resolves that one pattern against the effective
    /// graph.
    /// <para>
    /// The three rules below are not micro-optimisations; each was measured, and getting one
    /// wrong is either a silent wrong answer or a four-orders-of-magnitude regression. See
    /// <c>doc/adr/0041-layered-read-views.md</c> for the numbers.
    /// </para>
    /// </remarks>
    internal static class LayeredModelSparql
    {
        /// <summary>
        /// Wraps one triple pattern so it resolves against the effective graph rather than a
        /// single named graph.
        /// </summary>
        /// <remarks>
        /// Emits
        /// <c>{ { GRAPH baseline { T } &lt;guard&gt; } UNION { GRAPH additions { T } } }</c>.
        /// The guard applies only to the baseline branch, which is what makes additions win over
        /// removals.
        /// <para>
        /// <b>The guard is chosen by whether the pattern binds anything.</b> With at least one
        /// variable, <c>MINUS</c> is used: engines evaluate it as an anti-join instead of a
        /// per-row nested-loop probe, which is the difference between 1.5x and 18x on an
        /// unselective scan. But SPARQL <c>MINUS</c> removes nothing when the two sides share no
        /// variables, and a fully ground pattern has none — so there it would <b>silently fail to
        /// subtract</b> (verified: dotNetRDF and GraphDB both return the removed triple; Virtuoso
        /// happens not to). A ground pattern is a single existence check, so
        /// <c>FILTER NOT EXISTS</c> costs nothing there and is the only correct choice.
        /// </para>
        /// </remarks>
        /// <param name="model">The layered model supplying the three graph URIs.</param>
        /// <param name="subject">Serialized subject term — a variable (<c>?s</c>) or an RDF term.</param>
        /// <param name="predicate">Serialized predicate term.</param>
        /// <param name="object">Serialized object term.</param>
        /// <returns>A group graph pattern, braces included.</returns>
        internal static string Overlay(ILayeredModel model, string subject, string predicate, string @object)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            string triple = string.Concat(subject, " ", predicate, " ", @object);
            bool hasVariable = IsVariable(subject) || IsVariable(predicate) || IsVariable(@object);

            // The effective triple is in the baseline and not staged for removal...
            string baselineBranch = string.Format("{{ GRAPH {0} {{ {1} }} {2} }}",
                Graph(model.Baseline), triple, Guard(model, triple, hasVariable));

            // ...or it is staged for addition and the baseline branch did not already yield it.
            // Without that second condition the two branches overlap, and SPARQL UNION is a bag
            // union: a triple present in both the baseline and the additions would produce two
            // solutions where the view promises a set. Re-adding a triple that is already there is
            // exactly what "additions win" invites, so the overlap is the common case, not a corner
            // one - and it would show up as inflated COUNTs and duplicated rows, silently.
            string additionsBranch = string.Format(
                "{{ GRAPH {0} {{ {1} }} FILTER NOT EXISTS {{ GRAPH {2} {{ {1} }} {3} }} }}",
                Graph(model.Additions), triple, Graph(model.Baseline),
                GroundGuard(model, triple));

            return "{ " + baselineBranch + " UNION " + additionsBranch + " }";
        }

        /// <summary>
        /// The overlay for an already-serialized triple.
        /// </summary>
        /// <remarks>
        /// Used when synchronizing a materialized graph, where the triples are known ground values
        /// rather than a pattern being built up term by term.
        /// </remarks>
        internal static string Overlay(ILayeredModel model, string triple)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            string baselineBranch = string.Format("{{ GRAPH {0} {{ {1} }} {2} }}",
                Graph(model.Baseline), triple, GroundGuard(model, triple));

            string additionsBranch = string.Format(
                "{{ GRAPH {0} {{ {1} }} FILTER NOT EXISTS {{ GRAPH {2} {{ {1} }} {3} }} }}",
                Graph(model.Additions), triple, Graph(model.Baseline), GroundGuard(model, triple));

            return "{ " + baselineBranch + " UNION " + additionsBranch + " }";
        }

        /// <summary>
        /// The removals guard for the baseline branch.
        /// </summary>
        /// <remarks>
        /// <c>MINUS</c> where the pattern binds a variable: engines evaluate it as an anti-join rather
        /// than a per-row probe, which is the difference between 1.5x and 18x on an unselective scan.
        /// But SPARQL <c>MINUS</c> removes nothing when the two sides share no variables, and a fully
        /// ground pattern has none - so there it would <b>silently fail to subtract</b> (verified:
        /// dotNetRDF and GraphDB both return the removed triple; Virtuoso happens not to). A ground
        /// pattern is a single existence check, so <c>FILTER NOT EXISTS</c> costs nothing there and is
        /// the only correct choice.
        /// </remarks>
        private static string Guard(ILayeredModel model, string triple, bool hasVariable)
        {
            return hasVariable
                ? string.Format("MINUS {{ GRAPH {0} {{ {1} }} }}", Graph(model.Removals), triple)
                : string.Format("FILTER NOT EXISTS {{ GRAPH {0} {{ {1} }} }}", Graph(model.Removals), triple);
        }

        /// <summary>
        /// The removals guard as used inside a <c>NOT EXISTS</c>, where <c>MINUS</c> is never safe.
        /// </summary>
        /// <remarks>
        /// Nested inside <c>NOT EXISTS</c> the pattern is being tested for existence rather than
        /// joined, so a <c>MINUS</c> whose sides share no variables would remove nothing and invert the
        /// answer for a triple that is in the baseline, the removals and the additions at once.
        /// <c>FILTER NOT EXISTS</c> is used unconditionally, and costs little because the additions
        /// branch yields few rows to test.
        /// </remarks>
        private static string GroundGuard(ILayeredModel model, string triple)
        {
            return string.Format("FILTER NOT EXISTS {{ GRAPH {0} {{ {1} }} }}", Graph(model.Removals), triple);
        }

        /// <summary>
        /// The dataset clause for a layered read: <c>FROM NAMED</c> for each of the three graphs.
        /// </summary>
        /// <remarks>
        /// <c>FROM NAMED</c> rather than <c>FROM</c>, because the overlay addresses the graphs
        /// explicitly with <c>GRAPH</c> and must not have them merged into the default graph.
        /// Naming all three also pins the dataset, so no unrelated graph in the store can leak
        /// into a read.
        /// </remarks>
        internal static string NamedDatasetClause(ILayeredModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return string.Format("FROM NAMED {0} FROM NAMED {1} FROM NAMED {2} ",
                Graph(model.Baseline), Graph(model.Additions), Graph(model.Removals));
        }

        /// <summary>
        /// Binds a variable to one or more known subjects with <c>VALUES</c>.
        /// </summary>
        /// <remarks>
        /// This has to be emitted <b>before</b> the overlay it constrains. Restricting the
        /// subject afterwards with <c>FILTER (?s = ...)</c> instead leaves the engine to push the
        /// filter into a <c>UNION</c> containing an anti-join, which neither the in-memory engine
        /// nor GraphDB does — measured at 15x and 38x respectively, against 1.0x for the
        /// <c>VALUES</c> form. Binding up front turns every read into an indexed probe.
        /// </remarks>
        internal static string BindSubjects(string variable, IEnumerable<Uri> uris)
        {
            var result = new StringBuilder();

            result.Append("VALUES ").Append(variable).Append(" { ");

            foreach (Uri uri in uris)
            {
                result.Append(SparqlSerializer.SerializeUri(uri)).Append(' ');
            }

            result.Append("} ");

            return result.ToString();
        }

        /// <summary>
        /// Serializes a model's URI for use as a <c>GRAPH</c> or <c>FROM NAMED</c> operand.
        /// </summary>
        private static string Graph(IModel model)
        {
            return SparqlSerializer.SerializeUri(model.Uri);
        }

        /// <summary>
        /// Indicates whether a serialized term is a SPARQL variable rather than an RDF term.
        /// </summary>
        private static bool IsVariable(string term)
        {
            return !string.IsNullOrEmpty(term) && (term[0] == '?' || term[0] == '$');
        }
    }
}
