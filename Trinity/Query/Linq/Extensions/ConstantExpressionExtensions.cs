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

using System;
using System.Linq.Expressions;
using System.Xml;
using VDS.RDF;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    /// <summary>
    /// Extensions for the <c>ConstantExpression</c> type.
    /// </summary>
    public static class ConstantExpressionExtensions
    {
        /// <summary>
        /// Convert the expression into a <c>ConstantTerm</c>.
        /// </summary>
        /// <param name="constant">A constant expression.</param>
        /// <returns>A <c>ConstantTerm</c> object.</returns>
        public static ISparqlExpression AsSparqlExpression(this ConstantExpression constant)
        {
            return new ConstantTerm(constant.AsNode());
        }

        /// <summary>
        /// Convert the expression into a <c>IriExpression.</c>
        /// </summary>
        /// <param name="constant">A constant expression.</param>
        /// <returns>A <c>IriExpression</c> object.</returns>
        public static IriExpression AsIriExpression(this ConstantExpression constant)
        {
            return new IriExpression(constant.AsSparqlExpression());
        }

        /// <summary>
        /// Convert the expression into a <c>LiteralExpression</c>.
        /// </summary>
        /// <param name="constant">A constant expression.</param>
        /// <returns>A <c>LiteralExpression</c> object.</returns>
        public static LiteralExpression AsLiteralExpression(this ConstantExpression constant)
        {
            return new LiteralExpression(constant.AsSparqlExpression());
        }

        /// <summary>
        /// Convert the expression into a numeric expression.
        /// </summary>
        /// <param name="constant">A constant expression.</param>
        /// <returns>A <c>NumericExpression</c> object.</returns>
        public static NumericExpression AsNumericExpression(this ConstantExpression constant)
        {
            return new NumericExpression(constant.AsSparqlExpression());
        }

        /// <summary>
        /// Convert the expression into a node.
        /// </summary>
        /// <param name="constant">A constant expression.</param>
        /// <returns>A <c>Node</c> object.</returns>
        public static INode AsNode(this ConstantExpression constant)
        {
            if (typeof(Uri).IsAssignableFrom(constant.Type))
            {
                // If we have a URI constant, return a URI node.
                return new NodeFactory().CreateUriNode(constant.Value as Uri);
            }
            else if (XsdTypeMapper.HasXsdTypeUri(constant.Type) || constant.Value is string)
            {
                // If we have a literal value, return literal nodes.
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
            else if(typeof(Resource).IsAssignableFrom(constant.Type))
            {
                // If we have a constant of type Resource, return a URI node.
                Resource resource = constant.Value as Resource;

                return new NodeFactory().CreateUriNode(resource.Uri);
            }
            else
            {
                // We cannot determine the Uri of generic reference types.
                string msg = string.Format("Unsupported constant type: {0}", constant.Type);
                throw new ArgumentException(msg);
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

        /// <summary>
        /// Indicates if the expression can be evaluated to <c>false</c>.
        /// </summary>
        /// <param name="constant">A constant expression.</param>
        /// <returns><c>true</c> if the value is either <c>null</c> or <c>false</c>, <c>false</c> otherwise.</returns>
        public static bool IsNullOrFalse(this ConstantExpression constant)
        {
            return constant.Value == null || constant.Value is bool && ((bool)constant.Value) == false;
        }
    }
}
