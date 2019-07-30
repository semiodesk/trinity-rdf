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
// Copyright (c) Semiodesk GmbH 2015

#if NET35

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Utility
{
    /// <summary>
    /// A tuple is a finite sequence of elements.
    /// 
    /// This implementation is necessary because Net35 doesn't supply one.
    /// </summary>
    /// <typeparam name="T1">Type of the first item.</typeparam>
    /// <typeparam name="T2">Type of the second item.</typeparam>
    public class Tuple<T1, T2>
    {
#region Members
        /// <summary>
        /// The first item in the sequence.
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// The second item in the sequence.
        /// </summary>
        public T2 Item2 { get; private set; }
#endregion

#region Constructor
        /// <summary>
        /// The constructor, takes the items as parameters.
        /// </summary>
        /// <param name="item1">The first item</param>
        /// <param name="item2">The second item</param>
        public Tuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
#endregion

#region Methods

        /// <summary>
        /// Compares two tuples by comparing its items.
        /// </summary>
        /// <param name="obj">Another tuple</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Tuple<T1, T2>)
            {
                var x = (Tuple<T1, T2>)obj;
                return this.Item1.Equals(x.Item1) && this.Item2.Equals(x.Item2);
            }
            return false;
        }

        /// <summary>
        /// The hashcode of the tuple is the combined hashcode of the items.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Item1.GetHashCode() & this.Item2.GetHashCode();
        }

#endregion
    }

    /// <summary>
    /// A tuple is a finite sequence of elements.
    /// 
    /// This implementation is necessary because Net35 doesn't supply one.
    /// </summary>
    /// <typeparam name="T1">Type of the first item.</typeparam>
    /// <typeparam name="T2">Type of the second item.</typeparam>
    /// <typeparam name="T3">Type of the third item.</typeparam>
    public class Tuple<T1, T2, T3>
    {
#region Members
        /// <summary>
        /// The first item in the sequence.
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// The second item in the sequence.
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// The third item in the sequence.
        /// </summary>
        public T3 Item3 { get; private set; }
#endregion

#region Constructor
        /// <summary>
        /// The constructor, takes the items as parameters.
        /// </summary>
        /// <param name="item1">The first item</param>
        /// <param name="item2">The second item</param>
        /// <param name="item3">The third item</param>
        public Tuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
#endregion

#region Methods
        /// <summary>
        /// Compares two tuples by comparing its items.
        /// </summary>
        /// <param name="obj">Another tuple</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Tuple<T1, T2, T3>)
            {
                var x = (Tuple<T1, T2, T3>)obj;
                return this.Item1.Equals(x.Item1) && this.Item2.Equals(x.Item2) && this.Item3.Equals(x.Item3);
            }
            return false;
        }

        /// <summary>
        /// The hashcode of the tuple is the combined hashcode of the items.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Item1.GetHashCode() & this.Item2.GetHashCode() & this.Item3.GetHashCode();
        }
#endregion
    }

    /// <summary>
    /// A tuple is a finite sequence of elements.
    /// 
    /// This implementation is necessary because Net35 doesn't supply one.
    /// </summary>
    /// <typeparam name="T1">Type of the first item.</typeparam>
    /// <typeparam name="T2">Type of the second item.</typeparam>
    /// <typeparam name="T3">Type of the third item.</typeparam>
    /// <typeparam name="T4">Type of the fourth item.</typeparam>
    public class Tuple<T1, T2, T3, T4>
    {
#region Members
        /// <summary>
        /// The first item in the sequence.
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// The second item in the sequence.
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// The third item in the sequence.
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// The foruth item in the sequence.
        /// </summary>
        public T4 Item4 { get; private set; }
#endregion

#region Constructor
        /// <summary>
        /// The constructor, takes the items as parameters.
        /// </summary>
        /// <param name="item1">The first item</param>
        /// <param name="item2">The second item</param>
        /// <param name="item3">The third item</param>
        /// <param name="item4">The fourth item</param>
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
#endregion

#region Methods
        /// <summary>
        /// Compares two tuples by comparing its items.
        /// </summary>
        /// <param name="obj">Another tuple</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Tuple<T1, T2, T3, T4>)
            {
                var x = (Tuple<T1, T2, T3, T4>)obj;
                return this.Item1.Equals(x.Item1) && this.Item2.Equals(x.Item2) && this.Item3.Equals(x.Item3) && this.Item4.Equals(x.Item4);
            }
            return false;
        }

        /// <summary>
        /// The hashcode of the tuple is the combined hashcode of the items.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Item1.GetHashCode() & this.Item2.GetHashCode() & this.Item3.GetHashCode() & this.Item4.GetHashCode();
        }
#endregion
    }
}

#endif
