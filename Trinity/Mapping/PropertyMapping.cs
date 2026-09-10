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
using System.Collections.Generic;
using System.Collections;

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
        /// The datatype of the the mapped property.
        /// </summary>
        private readonly Type _dataType;

        /// <summary>
        /// The datatype of the the mapped property.
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
        /// </summary>
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
        /// Language of the value.
        /// </summary>
        public string Language { get; set; }

        private Property _property;

        /// <summary>
        /// Gets the mapped RDF property.
        /// </summary>
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

        /// <summary>
        /// Gets the URI of the mapped RDF property.
        /// </summary>
        public string PropertyUri { get; private set; }

        /// <summary>
        /// Gets the name of the mapped .NET property.
        /// </summary>
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

            RejectRawUriMapping(propertyName, property, _isList ? _genericType : _dataType);

#if DEBUG

            // Test if the given type is valid
            HashSet<Type> allowed = new HashSet<Type>{ typeof(string), 
                                                 typeof(bool),typeof(bool?), 
                                                 typeof(float), typeof(float?), 
                                                 typeof(double), typeof(double?),
                                                 typeof(decimal), typeof(decimal?),
                                                 typeof(Int16), typeof(Int16?),
                                                 typeof(Int32), typeof(Int32?),
                                                 typeof(Int64), typeof(Int64?),
                                                 typeof(UInt16), typeof(UInt16?),
                                                 typeof(UInt32), typeof(UInt32?),
                                                 typeof(UInt64), typeof(UInt64?),
                                                 typeof(DateTime), typeof(DateTime?),
                                                 typeof(TimeSpan), typeof(TimeSpan?),
                                                 typeof(System.Uri), typeof(Tuple<string, string>)};

            // Membership in 'allowed' is exact type identity, which rejects every subclass. That
            // used to make UriRef -- the type ADR-0025 tells callers to prefer for identity -- an
            // invalid mapping type, so the advice could not be followed. Uri subclasses are admitted
            // explicitly; the value paths treat them the same as Uri.
            bool IsCompatible(Type type)
            {
                return type != null
                    && (allowed.Contains(type)
                        || typeof(Uri).IsAssignableFrom(type)
                        || type.GetInterface("IResource") != null
                        || typeof(Resource).IsAssignableFrom(type));
            }

            if (!IsCompatible(_dataType))
            {
                // Test if type is an IList of a supported element type
                if (_dataType.GetInterface("IList") != null )
                {
                    // Test containing Type
                    if (IsCompatible(_genericType))
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

        /// <summary>
        /// Refuses a mapping whose value type is exactly <see cref="Uri"/> rather than
        /// <see cref="UriRef"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Uri.Equals(object)"/> ignores the fragment (RFC 3986), so
        /// <c>…/x#a</c> and <c>…/x#b</c> compare equal — in RDF those are two different resources.
        /// A mapping typed <see cref="Uri"/> therefore silently conflates values, and on .NET 10,
        /// where <see cref="Uri"/> gained <c>IEquatable&lt;Uri&gt;</c>, it does so in every generic
        /// collection as well. <see cref="UriRef"/> is assignable to <see cref="Uri"/>, so switching
        /// is a declaration change and nothing more.
        ///
        /// This backs up the generator's TRIN007: the generator only sees <c>partial</c> properties it
        /// emits, whereas mappings can also be declared by hand (ADR-0018), and those reach the runtime
        /// with no diagnostic at all. Unlike the compatibility check below it is not <c>#if DEBUG</c>,
        /// because a hand-written mapping in a Release build is exactly the case the generator misses.
        ///
        /// The throw surfaces wrapped by <c>MappingDiscovery.AddMappingClass</c> at registration time
        /// rather than at the property, which is why the message names the property itself.
        /// </remarks>
        /// <param name="propertyName">Name of the mapped .NET property.</param>
        /// <param name="property">The RDF property being mapped.</param>
        /// <param name="valueType">The mapped value type, or the element type for a list mapping.</param>
        private static void RejectRawUriMapping(string propertyName, Property property, Type valueType)
        {
            if (valueType != typeof(Uri))
            {
                return;
            }

            throw new ArgumentException(string.Format(
                "The property '{0}' mapped on RDF property '<{1}>' uses System.Uri. Use {2} instead: " +
                "System.Uri equality ignores the fragment, so two different RDF resources that differ " +
                "only by fragment would compare equal.",
                propertyName,
                property != null && property.Uri != null ? property.Uri.OriginalString : "?",
                typeof(UriRef).FullName));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">A value.</param>
        internal void SetValue(T value)
        {
            _isUnsetValue = false;
            _value = value;
        }

        /// <summary>
        /// Returns the property value.
        /// </summary>
        /// <returns>The value, if any.</returns>
        internal T GetValue()
        {
            return _value;
        }

        /// <summary>
        /// Sets a single literal value or adds a value to a property mapped to a value collection.
        /// </summary>
        /// <remarks>
        /// This method is meant to be called from the non-mapped interface. It replaces the current value if 
        /// it is mapped to one value, adds it if the property is mapped to a list.
        /// </remarks>
        /// <param name="value">The value.</param>
        void IPropertyMapping.SetOrAddMappedValue(object value)
        {
            if (_isList)
            {
                if (_value is IList list)
                {
                    Type t = value.GetType();

                    if (t == _genericType || _genericType.IsAssignableFrom(t))
                    {
                        list.Add(value);
                        _isUnsetValue = false;

                        return;
                    }
                    else if (t.IsValueType &&
                             NumericConversion.TryConvert(value, _genericType, out object convertedItem))
                    {
                        list.Add(convertedItem);
                        _isUnsetValue = false;

                        return;
                    }
                    else if (typeof(Uri).IsAssignableFrom(_genericType) && typeof(Resource).IsAssignableFrom(t))
                    {
                        list.Add((value as Resource).Uri);
                        _isUnsetValue = false;

                        return;
                    }
                    else if (_genericType == typeof(UriRef) && value is Uri uriItem)
                    {
                        list.Add(uriItem.ToUriRef());
                        _isUnsetValue = false;

                        return;
                    }
                }
            }
            else
            {
                Type t = value.GetType();

                if (t == _dataType || _dataType.IsAssignableFrom(t))
                {
                    _value = (T)value;
                    _isUnsetValue = false;

                    return;
                }
                else if (t.IsValueType && NumericConversion.TryConvert(value, _dataType, out object converted))
                {
                    _value = (T)converted;
                    _isUnsetValue = false;

                    return;
                }
                else if (typeof(Uri).IsAssignableFrom(_dataType) && typeof(Resource).IsAssignableFrom(t))
                {
                    _value = (T) (object)(value as Resource).Uri;
                    _isUnsetValue = false;

                    return;
                }
                // A plain Uri arriving at a UriRef mapping. Callers reach the unmapped surface with
                // raw Uri values -- IResource.AddProperty(property, Uri) takes one -- and TRIN007 plus
                // the constructor check now force every mapping to UriRef, so without this the value
                // would land in the unmapped bag and the mapped getter would return null.
                else if (_dataType == typeof(UriRef) && value is Uri uriValue)
                {
                    _value = (T) (object)uriValue.ToUriRef();
                    _isUnsetValue = false;

                    return;
                }
            }

            string typeString;

            if (_isList)
            {
                typeString = _genericType.ToString();
            }
            else
            {
                typeString = typeof(T).ToString();
            }

            string message = string.Format("Provided argument value was not of type {0}", typeString);

            throw new Exception(message);
        }

        /// <summary>
        /// Deletes the containing value and sets the state to unset. In case of a collection, it tries to remove the value from it.
        /// </summary>
        /// <param name="value"></param>
        void IPropertyMapping.RemoveOrResetValue(object value)
        {
            if (_isList)
            {
                // _genericType.IsAssignableFrom(value's type), not the reverse. The test used to be
                // inverted, and it failed in both directions.
                //
                // The one that mattered: it *rejected subclasses*, which is the ordinary polymorphic
                // case. Person.Interests is List<Resource>, so removing any Resource subclass through
                // the mapped interface threw "Provided argument value was not of type Resource". The
                // collection's own List<T>.Remove does not come through here, which is why the existing
                // tests never saw it.
                //
                // The other: it accepted *supertypes* -- a value that cannot possibly be in the list --
                // which then reached IList.Remove, where the internal type check drops the call. That
                // is a silent no-op, and a following Commit() re-persists the value the caller asked to
                // delete. Refused below instead, matching the scalar branch and this method's own
                // fallthrough.
                if (_genericType.IsAssignableFrom(value.GetType()))
                {
                    ((IList)_value).Remove(value);
                    return;
                }
                // Symmetric with SetOrAddMappedValue: a plain Uri widens into a UriRef collection.
                // Without this, a value that could be added could not be removed again.
                else if (_genericType == typeof(UriRef) && value is Uri uriItem)
                {
                    ((IList)_value).Remove(uriItem.ToUriRef());
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
                // As above: the add path widens a plain Uri into a UriRef mapping, so the remove path
                // has to accept the same value or the property can be set but never cleared.
                else if (_dataType == typeof(UriRef) && value is Uri)
                {
                    _value = default(T);
                    _isUnsetValue = true;
                    return;
                }
            }

            string typeString;

            if (_isList)
            {
                typeString = _genericType.ToString();
            }
            else
            {
                typeString = typeof(T).ToString();
            }

            string message = string.Format("Provided argument value was not of type {0}", typeString);
            
            throw new Exception(message);
        }

        /// <summary>
        /// Gets the value or values mapped to this property.
        /// </summary>
        /// <returns></returns>
        object IPropertyMapping.GetValueObject()
        {
            if (LanguageInvariant || string.IsNullOrEmpty(Language) && (_dataType != typeof(string) || _genericType != typeof(string)))
            {
                return _value;
            }
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
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();

            foreach (string v in _value as IList<string>)
            {
                result.Add(new Tuple<string, string>(v, Language));
            }

            return result;
        }

        /// <summary>
        /// Method to test if a type is compatible. In case of collection, the containing type is tested for compatibility.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>True if the type is compatible</returns>
        bool IPropertyMapping.IsTypeCompatible(Type type)
        {
            Type mappingType = _dataType;

            if (_isList)
            {
                mappingType = _genericType;
            }

            // NumericConversion is the single authority: the setter below converts with the same rules,
            // so the gate and the conversion cannot disagree. It also unwraps Nullable<T>, which the old
            // precision check did not — every nullable numeric property was unreadable as a result.
            if (NumericConversion.IsNumeric(type) && NumericConversion.IsNumeric(mappingType))
            {
                return NumericConversion.IsWideningTo(type, mappingType);
            }
            else
            {
                return (mappingType.IsAssignableFrom(type)
                    || typeof(Resource).IsAssignableFrom(mappingType) && typeof(Resource).IsAssignableFrom(type)
                    || (typeof(Uri).IsAssignableFrom(mappingType) && typeof(Resource).IsAssignableFrom(type))
                    // A plain Uri widens into a UriRef mapping; SetOrAddMappedValue performs the
                    // conversion, and this gate must agree with it or the value never reaches it.
                    || (mappingType == typeof(UriRef) && typeof(Uri).IsAssignableFrom(type)));
            }
        }

        /// <summary>
        /// Indicates if a particular value can be set on this mapping.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the value can be set.</returns>
        bool IPropertyMapping.IsValueCompatible(object value)
        {
            if (value == null)
            {
                return false;
            }

            Type mappingType = _isList ? _genericType : _dataType;

            // Numerics are value-aware: an exact integral narrowing is acceptable even though the types
            // alone would refuse it. Everything else falls back to the type-only question.
            if (NumericConversion.IsNumeric(value.GetType()) && NumericConversion.IsNumeric(mappingType))
            {
                return NumericConversion.CanConvert(value, mappingType);
            }

            return ((IPropertyMapping)this).IsTypeCompatible(value.GetType());
        }

        /// <summary>
        /// Indicates if the mapped value is a numeric type.
        /// </summary>
        /// <param name="type">A .NET type object.</param>
        /// <returns><c>true</c> if the type is numeric, <c>false</c> otherwise.</returns>
        public static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
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

        /// <summary>
        /// Clones the mapping of another resource.
        /// </summary>
        /// <param name="other"></param>
        void IPropertyMapping.CloneFrom(IPropertyMapping other)
        {
            if (_dataType != other.DataType)
            {
                return;
            }

            if (_value != null && _isList)
            {
                IList collection = (IList)_value;

                collection.Clear();

                IList otherCollection = (IList) other.GetValueObject();

                foreach (var v in otherCollection)
                {
                    collection.Add(v);
                }

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
