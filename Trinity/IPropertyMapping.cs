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

using System;

namespace Semiodesk.Trinity
{
    internal interface IPropertyMapping
    {
        /// <summary>
        /// The datatype of the the mapped property
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// If the datatype is a collection, this contains the generic type.
        /// </summary>
        Type GenericType { get; }

        /// <summary>
        /// True if the property is mapped to a collection.
        /// </summary>
        bool IsList { get; }

        /// <summary>
        /// The property that should be mapped.
        /// </summary>
        Property Property { get; }

        /// <summary>
        /// The name of the mapped property.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// True if the value has not been set.
        /// </summary>
        bool IsUnsetValue { get; }

        /// <summary>
        /// Language of the value
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// The mapping ignores the language setting and is always non-localized. Only valid if type or generic type is string or string collection.
        /// </summary>
        bool LanguageInvariant { get; }

        /// <summary>
        /// Method to test if a type is compatible. In case of collection, the containing type is tested for compatibility.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>True if the type is compatible</returns>
        bool IsTypeCompatible(Type type);

        /// <summary>
        /// Gets the value or values mapped to this property.
        /// </summary>
        /// <returns></returns>
        object GetValueObject();

        /// <summary>
        /// This method is meant to be called from the non-mapped interface. It replaces the current value if 
        /// it is mapped to one value, adds it if the property is mapped to a list.
        /// </summary>
        /// <param name="value"></param>
        void SetOrAddMappedValue(object value);

        /// <summary>
        /// Deletes the containing value and sets the state to unset. In case of a collection, it tries to remove the value from it.
        /// </summary>
        /// <param name="value"></param>
        void RemoveOrResetValue(object value);

        /// <summary>
        /// Clones the mapping of another resource.
        /// </summary>
        /// <param name="other"></param>
        void CloneFrom(IPropertyMapping other);

        /// <summary>
        /// Clears the mapping and resets it.
        /// </summary>
        void Clear();
    }
}
