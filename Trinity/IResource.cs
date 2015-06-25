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
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This interface encapsulates the access to the methods of a RDF resource.
    /// </summary>
    public interface IResource : INotifyPropertyChanged, IDisposable, ITransactional
    {
        #region Properties

        /// <summary>
        /// Uniform Resource Identifier (URI).
        /// </summary>
        UriRef Uri { get; set; }

        /// <summary>
        /// Model from which the resource was instantiated.
        /// </summary>
        IModel Model { get; }

        bool IsReadOnly { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A instance of IResource.</param>
        void AddProperty(Property property, IResource value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A string literal.</param>
        void AddProperty(Property property, string value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A string literal.</param>
        /// <param name="language">The language of the string literal.</param>
        void AddProperty(Property property, string value, CultureInfo language);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A 16-bit integer value.</param>
        void AddProperty(Property property, Int16 value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A 32-bit integer value.</param>
        void AddProperty(Property property, Int32 value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A 64-bit integer value.</param>
        void AddProperty(Property property, Int64 value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A 16-bit unsigned integer value.</param>
        void AddProperty(Property property, UInt16 value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A 32-bit unsigned integer value.</param>
        void AddProperty(Property property, UInt32 value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A 64-bit unsigned integer value.</param>
        void AddProperty(Property property, UInt64 value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A single precision float value.</param>
        void AddProperty(Property property, float value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A double precision float value.</param>
        void AddProperty(Property property, double value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A boolean value.</param>
        void AddProperty(Property property, bool value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">A datetime value.</param>
        void AddProperty(Property property, DateTime value);

        /// <summary>
        /// Adds a new property with the given value to the resource.
        /// </summary>
        /// <param name="property">Property the value should be associated with.</param>
        /// <param name="value">Arbitrary data in form of a byte array.</param>
        void AddProperty(Property property, byte[] value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">An IResource instance.</param>
        void RemoveProperty(Property property, IResource value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A instance of IResource.</param>
        void RemoveProperty(Property property, string value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A string literal.</param>
        void RemoveProperty(Property property, string value, CultureInfo language);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A 16-bit integer value.</param>
        void RemoveProperty(Property property, Int16 value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A 32-bit integer value.</param>
        void RemoveProperty(Property property, Int32 value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A 64-bit integer value.</param>
        void RemoveProperty(Property property, Int64 value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A 16-bit unsigned integer value.</param>
        void RemoveProperty(Property property, UInt16 value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A 32-bit unsigned integer value.</param>
        void RemoveProperty(Property property, UInt32 value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A 64-bit unsigned integer value.</param>
        void RemoveProperty(Property property, UInt64 value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A single precision float value.</param>
        void RemoveProperty(Property property, float value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A double precision float value.</param>
        void RemoveProperty(Property property, double value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A blooean value.</param>
        void RemoveProperty(Property property, bool value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">A date value.</param>
        void RemoveProperty(Property property, DateTime value);

        /// <summary>
        /// Removes an associated property from the resource.
        /// </summary>
        /// <param name="property">Property the given value is associated with.</param>
        /// <param name="value">Arbitrary data in form of a byte array.</param>
        void RemoveProperty(Property property, byte[] value);

        /// <summary>
        /// Indicates if the resource has at least one property of the given type.
        /// </summary>
        /// <param name="property"></param>
        /// <returns>True on success, False otherwise.</returns>
        bool HasProperty(Property property);

        /// <summary>
        /// Indicates if the resource has a property with the given value.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns>True on success, False otherwise.</returns>
        bool HasProperty(Property property, object value);

        /// <summary>
        /// Indicates if the resource has a property with the given translated string value.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="language"></param>
        /// <returns>True on success, False otherwise.</returns>
        bool HasProperty(Property property, string value, CultureInfo language);

        /// <summary>
        /// Enumerates all properties associated with this resource.
        /// </summary>

        /// <returns></returns>
        List<Property> ListProperties();

        /// <summary>
        /// Enumerates all properties associated with this resource in form 
        /// of a tuple mapping properties to their corresponding values.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        IEnumerable<Tuple<Property, object>> ListValues();

        /// <summary>
        /// Enumerates all property values associated with this resource.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        List<object> ListValues(Property property);

        /// <summary>
        /// Gets the value of a uniquely asserted property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        object GetValue(Property property);

        /// <summary>
        /// Gets the value of a uniquely asserted property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        object GetValue(Property property, object defaultValue);

        #endregion
    }
}
