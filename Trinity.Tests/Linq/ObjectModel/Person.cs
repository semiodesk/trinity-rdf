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
// Copyright (c) Semiodesk GmbH 2017

using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Test.Linq
{
    [RdfClass(FOAF.Person)]
    internal class Person : Agent
    {
        #region Members

        [RdfProperty(FOAF.age)]
        public int Age { get; set; }

        [RdfProperty(FOAF.birthday)]
        public DateTime Birthday { get; set; }

        [RdfProperty(FOAF.knows)]
        public List<Person> KnownPeople { get; set; }

        [RdfProperty(FOAF.member)]
        public Group Group { get; set; }

        [RdfProperty(FOAF.status)]
        public bool Status { get; set; }

        [RdfProperty(FOAF.account)]
        public float AccountBalance { get; set; }

        [RdfProperty(FOAF.interest)]
        public List<Resource> Interests { get; set; }

        [RdfProperty(FOAF.made)]
        public List<Document> Made { get; set; }

        #endregion

        #region Constructors

        public Person(Uri uri) : base(uri) {}

        #endregion
    }
}
