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
using System.Linq.Expressions;
using VDS.RDF;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    public static class ConstantExpressionExtensions
    {
        public static ISparqlExpression AsSparqlExpression(this ConstantExpression constantExpression)
        {
            string value = constantExpression.Value.ToString();

            if (constantExpression.Type == typeof(string))
            {
                return new ConstantTerm(new NodeFactory().CreateLiteralNode(value));
            }
            else
            {
                Uri datatype = XsdTypeMapper.GetXsdTypeUri(constantExpression.Type);

                return new ConstantTerm(new NodeFactory().CreateLiteralNode(value, datatype));
            }
        }

        public static INode AsNode(this ConstantExpression constantExpression)
        {
            string value = constantExpression.Value.ToString();

            if (constantExpression.Type == typeof(string))
            {
                return new NodeFactory().CreateLiteralNode(value);
            }
            else
            {
                Uri datatype = XsdTypeMapper.GetXsdTypeUri(constantExpression.Type);

                return new NodeFactory().CreateLiteralNode(value, datatype);
            }
        }
    }
}
