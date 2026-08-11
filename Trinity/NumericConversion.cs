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
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Decides whether a numeric value read from a store can land in a mapped property, and performs the
    /// conversion when it can.
    /// </summary>
    /// <remarks>
    /// A store may legitimately return a numeric literal whose CLR type is not the one the property is
    /// mapped to. <c>"400"^^xsd:decimal</c> is a valid lexical form and the value space of
    /// <c>xsd:decimal</c> contains the integers, so a store that canonicalizes it into an integer
    /// representation is within its rights — and non-Trinity writers exist regardless. Reading has to
    /// tolerate it.
    ///
    /// <b>Widening only.</b> Conversions that cannot lose information are performed; everything else is
    /// refused so the caller hears about it. In particular <c>Convert.ChangeType</c> is deliberately not
    /// used as the gate: it rounds (<c>3.7m</c> to <c>4</c> for an int), parses strings, and cannot target
    /// <c>Nullable&lt;T&gt;</c> at all — which was the original defect.
    ///
    /// This type is the single authority for the question. <see cref="PropertyMapping{T}"/> uses it both
    /// to answer "is this value acceptable" and to convert, so the gate and the setter cannot disagree.
    /// </remarks>
    internal static class NumericConversion
    {
        #region Members

        /// <summary>
        /// The widening conversions that are accepted, keyed by source type.
        /// </summary>
        /// <remarks>
        /// Deliberately absent, and therefore refused:
        /// <list type="bullet">
        ///   <item><description>All narrowing (<c>Decimal</c>→<c>Int32</c>, <c>Int64</c>→<c>Int16</c>,
        ///   <c>Double</c>→<c>Single</c>): loses information, and rounding silently is worse than
        ///   throwing.</description></item>
        ///   <item><description><c>Double</c>/<c>Single</c>→<c>Decimal</c> and <c>Decimal</c>→ any
        ///   floating type: different value spaces, lossy in both directions.</description></item>
        ///   <item><description>Signed to unsigned (<c>Int32</c>→<c>UInt32</c>): the negative range has
        ///   nowhere to go.</description></item>
        ///   <item><description><c>Int64</c>→<c>Double</c>, <c>UInt64</c>→<c>Double</c>,
        ///   <c>Int32</c>→<c>Single</c>: C# allows these implicitly but they lose precision above 2^53 and
        ///   2^24 respectively. This is stricter than C# on purpose — silent precision loss on large
        ///   integers is the failure class this type exists to prevent. <c>Int32</c>→<c>Double</c> and
        ///   <c>UInt32</c>→<c>Double</c> are exact and remain allowed.</description></item>
        /// </list>
        /// </remarks>
        private static readonly Dictionary<TypeCode, TypeCode[]> Widening =
            new Dictionary<TypeCode, TypeCode[]>
            {
                [TypeCode.SByte] = new[]
                {
                    TypeCode.Int16, TypeCode.Int32, TypeCode.Int64, TypeCode.Decimal
                },
                [TypeCode.Byte] = new[]
                {
                    TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32,
                    TypeCode.Int64, TypeCode.UInt64, TypeCode.Decimal
                },
                [TypeCode.Int16] = new[]
                {
                    TypeCode.Int32, TypeCode.Int64, TypeCode.Decimal
                },
                [TypeCode.UInt16] = new[]
                {
                    TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Decimal
                },
                [TypeCode.Int32] = new[]
                {
                    TypeCode.Int64, TypeCode.Double, TypeCode.Decimal
                },
                [TypeCode.UInt32] = new[]
                {
                    TypeCode.Int64, TypeCode.UInt64, TypeCode.Double, TypeCode.Decimal
                },
                [TypeCode.Int64] = new[] { TypeCode.Decimal },
                [TypeCode.UInt64] = new[] { TypeCode.Decimal },
                [TypeCode.Single] = new[] { TypeCode.Double }
            };

        #endregion

        #region Methods

        /// <summary>
        /// Indicates whether a value of one type can land in a property of another without losing
        /// information.
        /// </summary>
        /// <remarks>
        /// Both types are unwrapped from <see cref="Nullable{T}"/> first, so a nullable property is treated
        /// exactly like its underlying type. Failing to do that is what made every nullable numeric
        /// property unreadable when a store returned a different numeric type.
        /// </remarks>
        /// <param name="source">The type of the value that arrived.</param>
        /// <param name="target">The mapped property's type.</param>
        /// <returns><c>true</c> if the value can be converted without loss.</returns>
        public static bool IsWideningTo(Type source, Type target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            Type from = Unwrap(source);
            Type to = Unwrap(target);

            if (from == to)
            {
                return true;
            }

            return Widening.TryGetValue(Type.GetTypeCode(from), out TypeCode[] allowed) &&
                   Array.IndexOf(allowed, Type.GetTypeCode(to)) >= 0;
        }

        /// <summary>
        /// Converts a value to a mapped property's type, if that can be done without losing information.
        /// </summary>
        /// <param name="value">The value that arrived.</param>
        /// <param name="target">The mapped property's type, nullable or not.</param>
        /// <param name="converted">The converted value, boxed as the target's underlying type.</param>
        /// <returns><c>false</c> if the conversion would lose information, in which case the caller should
        /// refuse the value rather than store something inexact.</returns>
        public static bool TryConvert(object value, Type target, out object converted)
        {
            converted = null;

            if (value == null || target == null)
            {
                return false;
            }

            Type to = Unwrap(target);

            if (value.GetType() == to)
            {
                converted = value;

                return true;
            }

            if (!IsWideningTo(value.GetType(), to))
            {
                return false;
            }

            // Safe by construction: only conversions from the allowlist reach here, and the target is
            // always unwrapped, which Convert.ChangeType requires.
            converted = Convert.ChangeType(value, to);

            return true;
        }

        /// <summary>
        /// Indicates whether a type is one this class knows how to convert.
        /// </summary>
        /// <param name="type">A type, nullable or not.</param>
        /// <returns><c>true</c> for the numeric primitives and <c>decimal</c>.</returns>
        public static bool IsNumeric(Type type)
        {
            if (type == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(Unwrap(type)))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        private static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        #endregion
    }
}
