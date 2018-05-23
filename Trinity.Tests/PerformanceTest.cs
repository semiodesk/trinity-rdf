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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Diagnostics;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    public class PerformanceTest
    {
        #region Members

        IStore _store;

        IModel _model;

        #endregion

        [SetUp]
        public void SetUp()
        {
            _store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");

            UriRef uri = new UriRef("http://localhost:8899/models/PerformanceTest");

            if (_store.ContainsModel(uri))
            {
                _model = _store.GetModel(uri);
                _model.Clear();
            }
            else
            {
                _model = _store.CreateModel(uri);
            }

            if (_model == null)
            {
                throw new Exception(string.Format("Error: Unable to create model <{0}>.", uri));
            }

            if (_model.IsEmpty)
            {
                PersonContact c = null;

                for (int i = 1; i < 1000; i++)
                {
                    c = _model.CreateResource<PersonContact>();
                    c.Fullname = "Contact" + i;
                    c.BirthDate = DateTime.UtcNow;
                    c.Commit();
                }
            }
        }

        [Test]
        public void TestGenerateResources()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int i = 0;

            foreach (PersonContact c in _model.GetResources<PersonContact>())
            {
                i++;
            }

            stopwatch.Stop();
        }
    }
}
