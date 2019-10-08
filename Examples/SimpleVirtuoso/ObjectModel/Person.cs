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
// Copyright (c) Semiodesk GmbH 2015-2019

using Semiodesk.Trinity;
using System;
using System.Collections.ObjectModel;

namespace SimpleVirtuoso
{
    /// <summary>
    /// A person (alive, dead, undead, or fictional).
    /// </summary>
    [RdfClass(SCHEMA.Person)]
    public class Person : Thing
    {
        #region Members

        /// <summary>
        /// Given name. In the U.S., the first name of a Person. This can be used along 
        /// with familyName instead of the name property.
        /// </summary>
        [RdfProperty(SCHEMA.givenName)]
        public string FirstName { get; set; }

        /// <summary>
        /// Family name. In the U.S., the last name of an Person. This can be used along 
        /// with givenName instead of the name property.
        /// </summary>
        [RdfProperty(SCHEMA.familyName)]
        public string LastName { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [RdfProperty(SCHEMA.birthDate)]
        public DateTime BirthDate { get; set; }

        /// <summary>
        /// A child of the person.
        /// </summary>
        [RdfProperty(SCHEMA.children)]
        public ObservableCollection<Person> Children { get; set; } = new ObservableCollection<Person>();

        /// <summary>
        /// A parent of this person.
        /// </summary>
        [RdfProperty(SCHEMA.parent)]
        public ObservableCollection<Person> Parents { get; set; } = new ObservableCollection<Person>();

        #endregion

        #region Constructors

        public Person(Uri uri) : base(uri) {}

        #endregion
    }
}
