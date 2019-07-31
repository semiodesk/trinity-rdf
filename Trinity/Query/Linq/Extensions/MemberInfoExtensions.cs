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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Query
{
    /// <summary>
    /// Extensions for the <c>MemberInfo</c> type.
    /// </summary>
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Gets the first custom attribute of a specified type which is attached to a class member.
        /// </summary>
        /// <typeparam name="TAttribute">Custom attribute type.</typeparam>
        /// <param name="member">A class member.</param>
        /// <returns>A custom attribute object on success, <c>null</c> otherwise.</returns>
        public static TAttribute TryGetCustomAttribute<TAttribute>(this MemberInfo member) where TAttribute : Attribute
        {
            return member.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;
        }

        /// <summary>
        /// Get the .NET type of the given class member.
        /// </summary>
        /// <param name="member">A class member.</param>
        /// <returns>The class member type.</returns>
        public static Type GetMemberType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return (member as EventInfo).EventHandlerType;
                case MemberTypes.Field:
                    return (member as FieldInfo).FieldType;
                case MemberTypes.Method:
                    return (member as MethodInfo).ReturnType;
                case MemberTypes.Property:
                    return (member as PropertyInfo).PropertyType;
                default:
                    throw new ArgumentException("Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo");
            }
        }

        /// <summary>
        /// Indicates if the given member is of type <c>Uri</c> or a sub type.
        /// </summary>
        /// <param name="member">A class member.</param>
        /// <returns><c>true</c> if the member can be represented by a URI, <c>false</c> otherwise.</returns>
        public static bool IsUriType(this MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;

            if(property != null)
            {
                return typeof(Uri).IsAssignableFrom(property.PropertyType);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates if the given member is a built-in call.
        /// </summary>
        /// <param name="member">A class member.</param>
        /// <returns><c>true</c> if the class member is a built-in call, <c>false</c> otherwise.</returns>
        public static bool IsBuiltInCall(this MemberInfo member)
        {
            HashSet<Type> systemTypes = new HashSet<Type>()
            {
                typeof(DateTime),
                typeof(String)
            };

            return systemTypes.Contains(member.DeclaringType);
        }
    }
}
