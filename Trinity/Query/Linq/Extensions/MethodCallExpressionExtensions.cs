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

using System.Linq;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    public static class MethodCallExpressionExtensions
    {
        public static bool HasArgumentValue(this MethodCallExpression expression, int index, object value)
        {
            if (index < expression.Arguments.Count)
            {
                ConstantExpression arg = expression.Arguments[index] as ConstantExpression;

                return arg.Value == value;
            }

            return false;
        }

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

        public static T GetArgumentValue<T>(this MethodCallExpression expression, int index)
        {
            ConstantExpression arg = expression.Arguments[index] as ConstantExpression;

            return (T)arg.Value;
        }

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
