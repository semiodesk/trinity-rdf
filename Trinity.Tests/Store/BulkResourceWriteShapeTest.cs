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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Semiodesk.Trinity.Tests.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Pins the SPARQL a bulk wholesale write actually emits.
    /// </summary>
    /// <remarks>
    /// The behavioural tests catch a revert to the original single conjunctive <c>OPTIONAL</c>, but
    /// not the tempting half-fix of one <c>OPTIONAL</c> per resource. Each optional is independent
    /// there, so the absent-subject case stays green while the cross-product quietly returns and
    /// cost goes back to the product of the subjects' triple counts. Nor would they catch the
    /// insert being folded back into the modify, which duplicates blank-node values once per
    /// existing triple -- <c>BulkUpdateInsertsABlankNodeValuedLinkOnlyOnce</c> covers that, but only
    /// because a blank node makes the duplication visible; with IRI values the repeated triples
    /// collapse and nothing shows.
    ///
    /// So the query text is captured, as <see cref="BulkResourceQueryShapeTest"/> does for reads.
    /// The store's <c>Log</c> hook is used rather than <c>CapturingStore</c>, because
    /// <c>UpdateResources</c> is built inside the store and never crosses a wrapper's
    /// <c>ExecuteNonQuery</c>.
    /// </remarks>
    [TestFixture]
    public class BulkResourceWriteShapeTest
    {
        private IStore _store;

        private IModel _model;

        private readonly List<string> _updates = new List<string>();

        [SetUp]
        public void SetUp()
        {
            MappingDiscovery.RegisterAssembly(typeof(BulkResourceWriteShapeTest).Assembly);
            OntologyDiscovery.AddAssembly(typeof(BulkResourceWriteShapeTest).Assembly);

            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _model = _store.GetModel(new Uri("http://example.org/write-shape/model"));
            _model.Clear();

            _updates.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
        }

        /// <summary>
        /// The update emitted when several unsynchronized resources are written at once.
        /// </summary>
        private List<string> WholesaleUpdates(int resources)
        {
            var batch = new List<Resource>();

            for (var i = 0; i < resources; i++)
            {
                // Constructed rather than loaded: no baseline, so the wholesale branch is taken.
                var person = new Person(new UriRef($"http://example.org/write-shape/p{i}"));

                person.SetModel(_model);
                person.FirstName = $"P{i}";

                batch.Add(person);
            }

            _store.Log = s => _updates.Add(s);

            _model.UpdateResources(batch);

            _store.Log = null;

            return _updates.Where(u => u.Contains("DELETE")).ToList();
        }

        [Test]
        public void TheSubjectsAreBoundWithValuesAndTheShapeDoesNotGrowWithTheBatch()
        {
            var three = WholesaleUpdates(3).Single();

            StringAssert.Contains("VALUES", three);

            // The pattern appears twice by design -- once in the DELETE template, once in the WHERE
            // -- and what matters is that the count is fixed rather than one pair per resource.
            var patternsForThree = Regex.Matches(three, @"\?s \?p \?o").Count;

            SetUp();

            var six = WholesaleUpdates(6).Single();

            Assert.AreEqual(patternsForThree, Regex.Matches(six, @"\?s \?p \?o").Count,
                "the pattern count must not scale with the batch: a pattern per resource left-joins "
                + "into a cross product whatever shape the optionals take");

            Assert.IsFalse(Regex.IsMatch(six, @"\?p\d"),
                "numbered variables mean one pattern per resource, which is the form this replaced");

            Assert.IsFalse(six.Contains("OPTIONAL"),
                "the OPTIONAL existed only so the ground INSERT applied to a subject with no "
                + "triples; INSERT DATA is unconditional, so the delete matches only what it deletes");
        }

        [Test]
        public void TheInsertIsItsOwnOperationRatherThanATemplateInTheModify()
        {
            var update = WholesaleUpdates(3).Single();

            StringAssert.Contains("INSERT DATA", update);

            Assert.IsTrue(update.IndexOf(';') > update.IndexOf("DELETE"),
                "two operations in one request: the delete, then the insert");

            Assert.IsFalse(Regex.IsMatch(update, @"DELETE\s*\{[^}]*\}\s*INSERT\s*\{"),
                "an INSERT template inside the modify is instantiated once per solution, which mints "
                + "a fresh blank node each time");
        }

        [Test]
        public void SubjectsAreChunkedRatherThanEmittedAsOneUnboundedBlock()
        {
            var batchSize = SparqlSerializer.SubjectBindingBatchSize;

            var writes = WholesaleUpdates(batchSize + 1).Count;

            Assert.AreEqual(2, writes,
                $"{batchSize + 1} subjects must be written as two chunks, not one unbounded block");
        }
    }
}
