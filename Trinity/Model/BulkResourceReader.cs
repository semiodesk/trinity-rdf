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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The one implementation of "read these subjects as resources", shared by every
    /// <see cref="IModel"/>.
    /// </summary>
    /// <remarks>
    /// This exists because three copies of it were what let a defect survive a release. Each model
    /// had its own loop; the fix for the equality chain landed in two of them, and the third kept
    /// issuing one unbounded <c>VALUES</c> block until a review caught it (ADR-0046). The parts that
    /// genuinely differ between models are the query the binding goes into, how it is executed, and
    /// the flags stamped on each resource — so those are parameters, and nothing else is duplicated.
    /// <para>
    /// Batching is not optional and not a tuning knob: <c>VALUES</c> lifts the nesting limit that
    /// sinks an equality chain, but Virtuoso still refuses the 4095th operand of one block with
    /// <c>SP030</c>. Partitioning by subject is what makes the batches safe to concatenate — every
    /// triple of a given resource stays inside one batch.
    /// </para>
    /// </remarks>
    internal static class BulkResourceReader
    {
        /// <summary>
        /// Reads the given subjects, one query per batch.
        /// </summary>
        /// <param name="subjects">The subjects to read. Blank identifiers and nulls are skipped.</param>
        /// <param name="type">The type to materialize each resource as.</param>
        /// <param name="createQuery">Builds the query for one <c>VALUES</c> binding.</param>
        /// <param name="execute">Executes one query.</param>
        /// <param name="attach">Stamps a materialized resource with the model's flags.</param>
        internal static IEnumerable<object> Read(
            IEnumerable<Uri> subjects,
            Type type,
            Func<string, ISparqlQuery> createQuery,
            Func<ISparqlQuery, ISparqlQueryResult> execute,
            Action<Resource> attach)
        {
            foreach (string binding in SparqlSerializer.GenerateSubjectBindings("?s", subjects))
            {
                ISparqlQueryResult result = execute(createQuery(binding));

                foreach (Resource resource in result.GetResources(type))
                {
                    // A missing rdf:type yields a null entry rather than a resource.
                    if (resource == null)
                    {
                        continue;
                    }

                    attach(resource);

                    yield return resource;
                }
            }
        }
    }
}
