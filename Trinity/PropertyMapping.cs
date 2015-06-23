/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) Semiodesk GmbH 2015

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

namespace Semiodesk.Trinity
{
    internal interface IPropertyMapping
    {
        Type DataType { get; }

        Type GenericType { get; }

        bool IsList { get; }

        Property RdfProperty { get; }

        string PropertyName { get; }

        IEnumerable<string> RelatedProperties { get; }

        bool IsUnsetValue { get; }

        bool IsTypeCompatible(Type type);

        object GetValueObject();

        void SetOrAddMappedValue(object value);

        void RemoveOrResetValue(object value);

        void CloneFrom(IPropertyMapping other);
    }

    public class PropertyMapping<T> : IPropertyMapping
    {
        #region Members

        T _value;

        private readonly Type _dataType;

        Type IPropertyMapping.DataType 
        {
            get { return _dataType;  }
        }

        private readonly Type _genericType;

        Type IPropertyMapping.GenericType
        {
            get { return _genericType; }
        }

        private readonly bool _isList;

        bool IPropertyMapping.IsList
        {
            get { return _isList; }
        }

        private string _propertyString;
        private Property _rdfProperty;

        Property IPropertyMapping.RdfProperty
        {
            get 
            {
                if (_rdfProperty == null)
                {
                    _rdfProperty = OntologyDiscovery.GetProperty(_propertyString);
                }
                return _rdfProperty;  
            }
        }

        bool _isUnsetValue = true;

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

        IEnumerable<string> _relatedProperties;
        IEnumerable<string> IPropertyMapping.RelatedProperties
        {
            get { return _relatedProperties; }
        }

        public string PropertyName
        {
            get; private set;
        }

        #endregion

        #region Constructors

        public PropertyMapping(string propertyName, Property property)
        {
            if( string.IsNullOrEmpty(propertyName) )
            {
                throw new ArgumentException("Property name may not be empty in PropertyMapping object.");
            }

            _relatedProperties = new string[0];
            PropertyName = propertyName;
            _rdfProperty = property;

            _dataType = typeof(T);

            if (_dataType.GetInterface("IList") != null)
            {
                _isList = true;
                _genericType = _dataType.GetGenericArguments()[0];
            }
            else
            {
                _isList = false;
                _genericType = null;
            }

            
            #if DEBUG
            // Test if the given type is valid
            List<Type> allowed = new List<Type>{ typeof(string), typeof(bool), typeof(float), typeof(double), 
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

        public PropertyMapping(string propertyName, string propertyString) : this(propertyName, property: null)
        {
            _propertyString = propertyString;
        }

        public PropertyMapping(string propertyName, Property property, T defaultValue) : this(propertyName, property)
        {
            SetValue(defaultValue);
        }

        public PropertyMapping(string propertyName, string propertyString, T defaultValue)
            : this(propertyName, property: null)
        {
            _propertyString = propertyString;
        }


        public PropertyMapping(string propertyName, Property property, T defaultValue, IEnumerable<string> relatedProperties) :this(propertyName, property, defaultValue)
            
        {
            _relatedProperties = relatedProperties;
        }

        
        #endregion

        #region Methods

        internal void SetValue(T value)
        {
            _isUnsetValue = false;
            _value = value;
        }

        internal T GetValue()
        {
            return _value;
        }

        #region IPropertyMapping

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

        object IPropertyMapping.GetValueObject()
        {
            return _value;
        }

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

        #endregion

        #endregion
    }
}
