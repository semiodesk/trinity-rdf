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

using System.Linq;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    /// <summary>
    /// Extensions for the <c>MethodCall</c> type.
    /// </summary>
    public static class MethodCallExpressionExtensions
    {
        /// <summary>
        /// Indicates if the method call has an argument with a specified value at a specified location.
        /// </summary>
        /// <param name="expression">A method call expression.</param>
        /// <param name="index">Location of the argument.</param>
        /// <param name="value">Value of the argument.</param>
        /// <returns><c>true</c> if the method call has an argument with the given value, <c>false</c> otherwise.</returns>
        public static bool HasArgumentValue(this MethodCallExpression expression, int index, object value)
        {
            if (index < expression.Arguments.Count)
            {
                ConstantExpression arg = expression.Arguments[index] as ConstantExpression;

                return arg.Value == value;
            }

            return false;
        }

        /// <summary>
        /// Indicates if the method call has an argument at a speficied loaction with one of the specified values.
        /// </summary>
        /// <param name="expression">A method call expression.</param>
        /// <param name="index">Location of the argument.</param>
        /// <param name="values">Values of the argument.</param>
        /// <returns><c>true</c> if the method call has an argument with one of the given values, <c>false</c> otherwise.</returns>
        public static bool HasArgumentValueFromAlternatives(this MethodCallExpression expression, int index, params object[] values)
        {
            if (index < expression.Arguments.Count)
            {
                ConstantExpression arg = expression.Arguments[index] as ConstantExpression;

                if(arg.Value != null)
                {
                    return values.Any(v => arg.Value.Equals(v));
                }
                else
                {
                    return values.Any(v => arg.Value == v);
                }
            }

            return false;
        }

        /// <summary>
        /// Get the value of an argument at the specified location.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="expression">A method call expression.</param>
        /// <param name="index">Argument location.</param>
        /// <returns></returns>
        public static T GetArgumentValue<T>(this MethodCallExpression expression, int index)
        {
            ConstantExpression arg = expression.Arguments[index] as ConstantExpression;

            return (T)arg.Value;
        }

        /// <summary>
        /// Get the value of an argument at the specified location.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="expression">A method call expression.</param>
        /// <param name="index">Argument location.</param>
        /// <param name="defaultValue">Value to be returned if no argument is specified at the given location.</param>
        /// <returns></returns>
        public static T GetArgumentValue<T>(this MethodCallExpression expression, int index, T defaultValue)
        {
            if (index < expression.Arguments.Count)
            {
                ConstantExpression arg = expression.Arguments[index] as ConstantExpression;

                return (T)arg.Value;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
