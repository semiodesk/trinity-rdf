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
// Copyright (c) Semiodesk GmbH 2015-2020

using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;

namespace Semiodesk.Trinity.Tests.Linq
{
    // The same 51 LinqTestBase bodies, re-run against the new SPARQL LINQ provider
    // (AsSparqlQueryable) instead of re-linq. This is the parity gate for the provider rebuild:
    // the operator breadth is "done" when these go green (ADR: LINQ provider).

    [TestFixture]
    [Explicit("WIP: SPARQL LINQ provider is under construction (operator breadth). Run by filter; " +
              "remove [Explicit] when the parity suite is green (A4).")]
    public class LinqSparqlModelTest : LinqModelTest
    {
        protected override IQueryable<T> Query<T>(bool inferenceEnabled = false)
        {
            return Model.AsSparqlQueryable<T>(inferenceEnabled);
        }
    }

    [TestFixture]
    [Explicit("WIP: SPARQL LINQ provider is under construction (operator breadth). Run by filter; " +
              "remove [Explicit] when the parity suite is green (A4).")]
    public class LinqSparqlModelGroupTest : LinqModelGroupTest
    {
        protected override IQueryable<T> Query<T>(bool inferenceEnabled = false)
        {
            return Model.AsSparqlQueryable<T>(inferenceEnabled);
        }
    }
}
