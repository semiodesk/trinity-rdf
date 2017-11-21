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
// Moritz Eberl <moritz@semiodesk.com>
// Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2017

using System;
using System.Collections.Generic;
using System.Reflection;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlVariableGenerator
    {
        #region Members

        private readonly Dictionary<string, int> _variableCounters = new Dictionary<string, int>();

        #endregion

        #region Methods

        public SparqlVariable GetVariable(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            int n = 0;

            if(_variableCounters.ContainsKey(name))
            {
                n = _variableCounters[name] + 1;

                _variableCounters[name] = n;
            }

            _variableCounters[name] = n;

            return new SparqlVariable(name + n);
        }

        public SparqlVariable GetGlobalVariable(string name, bool isResultVariable = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return new SparqlVariable(name.ToCamelCase() + "_", isResultVariable);
        }

        public SparqlVariable GetMemberVariable(MemberInfo member)
        {
            return GetVariable(member.Name.ToCamelCase());
        }

        public SparqlVariable GetPredicateVariable(string name = "p")
        {
            return GetVariable(name);
        }

        public SparqlVariable GetObjectVariable(string name = "o")
        {
            return GetVariable(name);
        }

        #endregion
    }
}
