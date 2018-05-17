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

using System.Collections.Generic;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal interface ISparqlVariableGenerator
    {
        #region Members

        Dictionary<string, int> VariableCounters { get; }

        SparqlVariable GlobalSubject { get; }

        SparqlVariable GlobalPredicate { get; }

        SparqlVariable GlobalObject { get; }

        #endregion

        #region Methods

        void AddVariableMapping(Expression expression, string alias);

        bool HasSubjectVariable(Expression expression);

        bool HasPredicateVariable(Expression expression);

        bool HasObjectVariable(Expression expression);

        SparqlVariable TryGetSubjectVariable(Expression expression);

        SparqlVariable TryGetPredicateVariable(Expression expression);

        SparqlVariable TryGetObjectVariable(Expression expression);

        void SetSubjectVariable(Expression expression, SparqlVariable s);

        void SetPredicateVariable(Expression expression, SparqlVariable p);

        void SetObjectVariable(Expression expression, SparqlVariable o);

        SparqlVariable CreateSubjectVariable(Expression expression);

        SparqlVariable CreatePredicateVariable();

        SparqlVariable CreateObjectVariable();

        SparqlVariable CreateObjectVariable(Expression expression);

        #endregion
    }
}
