﻿// LICENSE:
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

using Remotion.Linq;
using Remotion.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;

namespace Semiodesk.Trinity.Query
{
    internal interface ISparqlQueryGenerator
    {
        #region Members

        bool IsBound { get; }

        SparqlVariable SubjectVariable { get; }

        SparqlVariable ObjectVariable { get; }

        IList<SparqlVariable> SelectedVariables { get; }

        #endregion

        #region Methods

        void SetQueryModel(QueryModel queryModel);

        void SetVariableGenerator(SparqlVariableGenerator variableGenerator);

        void SetObjectOperator(ResultOperatorBase resultOperator);

        void SetObjectVariable(SparqlVariable variable, bool select = false);

        void SetSubjectVariable(SparqlVariable variable, bool select = false);

        void DeselectVariable(SparqlVariable variable);

        void SelectVariable(SparqlVariable variable);

        void Select(Expression selector, bool isRootQuery);

        void Where(Action<ITriplePatternBuilder> buildTriplePatterns);

        void Where(SparqlVariable subject, Type type);

        void Where(MemberExpression member);

        void WhereEqual(SparqlVariable variable, ConstantExpression constant);

        void WhereEqual(MemberExpression member, ConstantExpression constant);

        void WhereNotEqual(SparqlVariable variable, ConstantExpression constant);

        void WhereNotEqual(MemberExpression member, ConstantExpression constant);

        void WhereGreaterThan(SparqlVariable variable, ConstantExpression constant);

        void WhereGreaterThan(MemberExpression member, ConstantExpression constant);

        void WhereGreaterThanOrEqual(SparqlVariable variable, ConstantExpression constant);

        void WhereGreaterThanOrEqual(MemberExpression member, ConstantExpression constant);

        void WhereLessThan(SparqlVariable variable, ConstantExpression constant);

        void WhereLessThan(MemberExpression member, ConstantExpression constant);

        void WhereLessThanOrEqual(SparqlVariable variable, ConstantExpression constant);

        void WhereLessThanOrEqual(MemberExpression member, ConstantExpression constant);

        void OrderBy(SparqlVariable variable);

        void OrderByDescending(SparqlVariable variable);

        void Optional(Action<IGraphPatternBuilder> buildGraphPattern);

        void Offset(int offset);

        void Limit(int limit);

        string BuildQuery();

        #endregion
    }
}