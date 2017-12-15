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
using System.Xml;
using VDS.RDF;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    public static class ConstantExpressionExtensions
    {
        public static ISparqlExpression AsSparqlExpression(this ConstantExpression constant)
        {
            string value = GetValue(constant);
            Uri datatype = GetDataType(constant);

            if(datatype == null)
            {
                return new ConstantTerm(new NodeFactory().CreateLiteralNode(value));
            }
            else
            {
                return new ConstantTerm(new NodeFactory().CreateLiteralNode(value, datatype));
            }
        }

        public static LiteralExpression AsLiteralExpression(this ConstantExpression constant)
        {
            return new LiteralExpression(constant.AsSparqlExpression());
        }

        public static NumericExpression AsNumericExpression(this ConstantExpression constant)
        {
            return new NumericExpression(constant.AsSparqlExpression());
        }

        public static INode AsNode(this ConstantExpression constant)
        {
            string value = GetValue(constant);
            Uri datatype = GetDataType(constant);

            if (datatype == null)
            {
                return new NodeFactory().CreateLiteralNode(value);
            }
            else
            {
                return new NodeFactory().CreateLiteralNode(value, datatype);
            }
        }

        private static string GetValue(ConstantExpression constant)
        {
            if(constant.Type == typeof(DateTime))
            {
                return XmlConvert.ToString((DateTime)constant.Value, XmlDateTimeSerializationMode.Utc);
            }
            else if(constant.Type == typeof(bool))
            {
                return XmlConvert.ToString((bool)constant.Value);
            }
            else
            {
                return constant.Value.ToString();
            }
        }

        private static Uri GetDataType(ConstantExpression constant)
        {
            if(constant.Type == typeof(string))
            {
                return null;
            }
            else
            {
                return XsdTypeMapper.GetXsdTypeUri(constant.Type);
            }
        }
    }
}
