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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class does the heavy lifting of the property mapping mechanism. It stores the value and acts as intermediary for the resource.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyMapping<T> : IPropertyMapping
    {
        #region Members

        /// <summary>
        /// The value of the mapped property.
        /// </summary>
        private T _value;

        /// <summary>
        /// The datatype of the the mapped property
        /// </summary>
        private readonly Type _dataType;

        /// <summary>
        /// The datatype of the the mapped property
        /// </summary>
        Type IPropertyMapping.DataType 
        {
            get { return _dataType;  }
        }

        /// <summary>
        /// If the datatype is a collection, this contains the generic type.
        /// </summary>
        private readonly Type _genericType;

        /// <summary>
        /// If the datatype is a collection, this contains the generic type.
        /// </summary
        Type IPropertyMapping.GenericType
        {
            get { return _genericType; }
        }

        /// <summary>
        /// True if the property is mapped to a collection.
        /// </summary>
        private readonly bool _isList;

        /// <summary>
        /// True if the property is mapped to a collection.
        /// </summary>
        bool IPropertyMapping.IsList
        {
            get { return _isList; }
        }

        /// <summary>
        /// True if the value has not been set.
        /// </summary>
        private bool _isUnsetValue = true;

        /// <summary>
        /// True if the value has not been set.
        /// </summary>
        bool IPropertyMapping.IsUnsetValue
        {
            get
            {
                if (_isList && _value != null)
                {
                    return (_value as IList).Count == 0;
                }
                else
                {
                    return _isUnsetValue;
                }
            }
        }

        /// <summary>
        /// Language of the value
        /// </summary>
        public string Language { get; set; }

        private Property _property;

        Property IPropertyMapping.Property
        {
            get 
            {
                if (_property == null)
                {
                    _property = OntologyDiscovery.GetProperty(PropertyUri);
                }

                return _property;  
            }
        }

        public string PropertyUri { get; private set; }

        public string PropertyName { get; private set; }

        /// <summary>
        /// Only valid if type or generic type is string. The mapping ignores the language setting and is always non-localized.
        /// </summary>
        public bool LanguageInvariant { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new property mapping.
        /// </summary>
        /// <param name="propertyName">Name of the property in the class</param>
        /// <param name="property">The RDF property that should be mapped</param>
        /// <param name="languageInvariant">This parameter is only valid if the type is string. Tells the mapping that the values should be treated as non-localized literals.</param>
        public PropertyMapping(string propertyName, Property property, bool languageInvariant=false)
        {
            if( string.IsNullOrEmpty(propertyName) )
            {
                throw new ArgumentException("Property name may not be empty in PropertyMapping object.");
            }

            _property = property;

            LanguageInvariant = languageInvariant;

            PropertyName = propertyName;

            _dataType = typeof(T);

            if (_dataType.GetInterface("IList") != null)
            {
                _isList = true;
                _genericType = _dataType.GetGenericArguments()[0];
                _value = (T)Activator.CreateInstance(typeof(T));
            }
            else
            {
                _isList = false;
                _genericType = null;
            }

            
            #if DEBUG

            // Test if the given type is valid
            HashSet<Type> allowed = new HashSet<Type>{ typeof(string), typeof(bool), typeof(float), typeof(double), typeof(decimal),
                                                 typeof(Int16), typeof(Int32), typeof(Int64), 
                                                 typeof(UInt16),typeof(UInt32), typeof(UInt64), 
                                                 typeof(DateTime)};

            if (!allowed.Contains(_dataType) && _dataType.GetInterface("IResource") == null && !typeof(Resource).IsAssignableFrom(_dataType))
            {
                // Test if type is IList interface and INotifyCollectionChanged
                if (_dataType.GetInterface("IList") != null )
                {
                    // Test containing Type
                    if (allowed.Contains(_genericType) || _genericType.GetInterface("IResource") != null || typeof(Resource).IsAssignableFrom(_genericType))
                    {
                        return;
                    }
                }

                throw new Exception(string.Format("The property '{0}' with type {1} mapped on RDF property '<{2}>' is not compatible.", propertyName, _dataType, property));
            }

            #endif
        }

        /// <summary>
        /// Creates a new property mapping.
        /// </summary>
        /// <param name="propertyName">Name of the property in the class</param>
        /// <param name="property">The RDF property that should be mapped</param>
        /// <param name="defaultValue">The default value used to initialize this property</param>
        /// <param name="languageInvariant">This parameter is only valid if the type is string. Tells the mapping that the values should be treated as non-localized literals.</param>
        public PropertyMapping(string propertyName, Property property, T defaultValue, bool languageInvariant = false) : this(propertyName, property, languageInvariant)
        {
            SetValue(defaultValue);
        }

        /// <summary>
        /// Creates a new property mapping.
        /// </summary>
        /// <param name="propertyName">Name of the property in the class</param>
        /// <param name="propertyUri">The URI of the RDF property that should be mapped</param>
        /// <param name="languageInvariant">This parameter is only valid if the type is string. Tells the mapping that the values should be treated as non-localized literals.</param>
        public PropertyMapping(string propertyName, string propertyUri, bool languageInvariant = false)
            : this(propertyName, property: null, languageInvariant: languageInvariant)
        {
            PropertyUri = propertyUri;
        }

        /// <summary>
        /// Creates a new property mapping.
        /// </summary>
        /// <param name="propertyName">Name of the property in the class</param>
        /// <param name="propertyUri">The URI of the RDF property that should be mapped</param>
        /// <param name="defaultValue">The default value used to initialize this property</param>
        /// <param name="languageInvariant">This parameter is only valid if the type is string. Tells the mapping that the values should be treated as non-localized literals.</param>
        public PropertyMapping(string propertyName, string propertyUri, T defaultValue, bool languageInvariant = false)
            : this(propertyName, property: null, defaultValue: defaultValue, languageInvariant: languageInvariant)
        {
            PropertyUri = propertyUri;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the value
        /// </summary>
        /// <param name="value"></param>
        internal void SetValue(T value)
        {
            _isUnsetValue = false;
            _value = value;
        }

        /// <summary>
        /// Returns the value.
        /// </summary>
        /// <returns></returns>
        internal T GetValue()
        {
            return _value;
        }

        /// <summary>
        /// This method is meant to be called from the non-mapped interface. It replaces the current value if 
        /// it is mapped to one value, adds it if the property is mapped to a list.
        /// </summary>
        /// <param name="value"></param>
        void IPropertyMapping.SetOrAddMappedValue(object value)
        {
            if (_isList)
            {
                if (_value != null && value.GetType().IsAssignableFrom(_genericType))
                {
                    (_value as IList).Add(value);
                    _isUnsetValue = false;
                    return;
                }
            }
            else
            {
                if (typeof(T).IsAssignableFrom(value.GetType()))
                {
                    _value = (T)value;
                    _isUnsetValue = false;
                    return;
                }
            }

            string typeString;

            if (_isList)
                typeString = _genericType.ToString();
            else
                typeString = typeof(T).ToString();

            string exceptionMessage = string.Format("Provided argument value was not of type {0}", typeString);
            
            throw new Exception(exceptionMessage);
        }

        /// <summary>
        /// Deletes the containing value and sets the state to unset. In case of a collection, it tries to remove the value from it.
        /// </summary>
        /// <param name="value"></param>
        void IPropertyMapping.RemoveOrResetValue(object value)
        {
            if (_isList)
            {
                if (value.GetType().IsAssignableFrom(_genericType))
                {
                    ((IList)_value).Remove(value);
                    return;
                }
            }
            else
            {
                if (typeof(T).IsAssignableFrom(value.GetType()))
                {
                    _value = default(T);
                    _isUnsetValue = true;
                    return;
                }
            }

            string typeString;
            if (_isList)
                typeString = _genericType.ToString();
            else
                typeString = typeof(T).ToString();
            string exceptionMessage = string.Format("Provided argument value was not of type {0}", typeString);
            
            throw new Exception(exceptionMessage);
        }

        /// <summary>
        /// Gets the value or values mapped to this property.
        /// </summary>
        /// <returns></returns>
        object IPropertyMapping.GetValueObject()
        {
            if (LanguageInvariant || string.IsNullOrEmpty(Language) && ( _dataType != typeof(string) || _genericType != typeof(string)))
                return _value;
            else
            {
                if (_isList)
                {
                    return ToLanguageList();
                }
                else
                {
                    return new Tuple<string, string>(_value as string, Language);
                }
            }
        }

        /// <summary>
        /// Gets a list of strings as list of tuples containing the values and the language tags.
        /// </summary>
        /// <returns></returns>
        IList ToLanguageList()
        {
            List<Tuple<string, string>> res = new List<Tuple<string, string>>();
            foreach (string x in _value as IList<string>)
            {
                res.Add(new Tuple<string, string>(x, Language));
            }

            return res;
        }

        /// <summary>
        /// Method to test if a type is compatible. In case of collection, the containing type is tested for compatibility.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>True if the type is compatible</returns>
        bool IPropertyMapping.IsTypeCompatible(Type type)
        {
            if (_isList)
            {
                return (_genericType.IsAssignableFrom(type) || typeof(Resource).IsAssignableFrom(_genericType) && typeof(Resource).IsAssignableFrom(type));
            }
            else
            {
                return (_dataType.IsAssignableFrom(type) || typeof(Resource).IsAssignableFrom(_dataType) && typeof(Resource).IsAssignableFrom(type));
            }
        }

        /// <summary>
        /// Clones the mapping of another resource.
        /// </summary>
        /// <param name="other"></param>
        void IPropertyMapping.CloneFrom(IPropertyMapping other)
        {
            
            if (this._dataType != other.DataType)
                return;

            if (_value != null && _isList)
            {
                IList collection = (IList)_value;
                collection.Clear();
                IList otherCollection = (IList) other.GetValueObject();
                foreach (var v in otherCollection)
                    collection.Add(v);
                _isUnsetValue = other.IsUnsetValue;
            }
            else
            {
                _value = (T)other.GetValueObject();
                _isUnsetValue = other.IsUnsetValue;
            }
            
        }

        /// <summary>
        /// Clears the mapping and resets it.
        /// </summary>
        void IPropertyMapping.Clear()
        {
            if (_isList)
            {
                (_value as IList).Clear();
            }
            else
            {
                _value = default(T);
            }
            _isUnsetValue = true;
        }

        #endregion
    }
}
