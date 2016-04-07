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
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Collections;
using Newtonsoft.Json;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class repesents a RDF resource. 
    /// </summary>
    public class Resource : IResource
    {
        #region Members

        /// <summary>
        /// The cache for the associated resources, needed to support lazy loading for mapping.
        /// </summary>
        internal ResourceCache ResourceCache;

        /// <summary>
        /// This dictionary contains the properties and the associated values.
        /// </summary>
        private Dictionary<Property, HashSet<object>> _properties;

        /// <summary>
        /// Contains a list of all properties which implement the INotifyPropertyChanged interface.
        /// </summary>
        private readonly HashSet<string> _notifyingProperties = new HashSet<string>();

        /// <summary>
        /// All mappings as discovered by InitialisePropertyMapping.
        /// </summary>
        private Dictionary<string, IPropertyMapping> _mappings;

        /// <summary>
        /// Handle to the model.
        /// </summary>
        private IModel _model;

        /// <summary>
        /// Public accessor to the model.
        /// </summary>
        public IModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                ResourceCache.Model = value;
            }
        }

        /// <summary>
        /// The uri which represents the resource.
        /// </summary>
        public UriRef Uri { get; set; }

        /// <summary>
        /// New resource which have never been committed need to be treated differently.
        /// </summary>
        [JsonIgnore]
        public bool IsNew { get; set; }

        /// <summary>
        /// Indicates if the resources has been disposed.
        /// </summary>
        [JsonIgnore]
        protected bool IsDisposed;

        /// <summary>
        /// True if the properties of the resources has been committed to the model.
        /// </summary>
        [Browsable(false), JsonIgnore]
        public bool IsSynchronized { get; set; }

        /// <summary>
        /// Indicates this resource is read-only.
        /// </summary>
        [JsonIgnore]
        public bool IsReadOnly { get; internal set; }

        private string _language;
        [JsonIgnore]
        public string Language
        {
            get
            {
                return _language;
            }
            set
            {
                if (value != null)
                    _language = value.ToLower();
                else
                    _language = null;
                ReloadLocalizedMappings();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Private since a Resource cannot be created without a URI.
        /// </summary>
        private Resource()
        {
        }

        /// <summary>
        /// Create a new resource with a given Uri.
        /// </summary>
        /// <param name="uri"></param>
        public Resource(UriRef uri)
        {
            Initialize(uri);
        }

        /// <summary>
        /// Create a new resource with a given Uri.
        /// </summary>
        /// <param name="uri"></param>
        public Resource(Uri uri)
        {
            Initialize(uri.ToUriRef());
        }

        /// <summary>
        /// Create a new resource with a given string. Throws an exception if string is Uri compatible.
        /// </summary>
        /// <param name="uriString">The string converted to a Uri. Throws an exception if not possible.</param>
        public Resource(string uriString)
        {
            Initialize(new UriRef(uriString));
        }

        /// <summary>
        /// Create a new instance of the class and copy the properties from another class instance.
        /// </summary>
        /// <param name="other"></param>
        public Resource(Resource other)
        {
            _model = other._model;

            _properties = other._properties;
            _mappings = other._mappings;

            Uri = other.Uri;
            ResourceCache = other.ResourceCache;

            IsNew = other.IsNew;
            IsSynchronized = other.IsSynchronized;
        }

        #endregion

        #region Destructors

        ~Resource()
        {
            if (!IsDisposed)
            {
                Dispose();
            }
        }

        #endregion

        #region Methods

        internal void SetModel(IModel model)
        {
            Model = model;
        }

        private void Initialize(UriRef uri)
        {
            _model = null;
            _properties = new Dictionary<Property, HashSet<object>>();
            _mappings = new Dictionary<string, IPropertyMapping>();

            ResourceCache = new ResourceCache();

            Uri = uri;

            IsNew = true;
            IsSynchronized = false;

            InitializePropertyMappings();

            // We cannot register the resource with the model, if no model is set.
        }

        /// <summary>
        /// Loads and initialises all mapped properties. 
        /// </summary>
        /// <remarks>
        /// TODO: This method could be re-reimplemented and sped up by the CIL generator.
        /// </remarks>
        private void InitializePropertyMappings()
        {
            Type thisType = GetType();

            FieldInfo[] b = thisType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            IEnumerable<object> mappings = from n in b where (n.FieldType.GetInterface("IPropertyMapping") != null) select n.GetValue(this);

            foreach (IPropertyMapping mapping in mappings)
            {
                if (mapping != null)
                    _mappings.Add(mapping.PropertyName, mapping);
#if DEBUG
                else
                    Debug.WriteLine(string.Format("Mapping resulted in zero results in {0}", thisType.Name));
#endif
            }
        }

        internal void ClearListPropertyMappings()
        {
            foreach (var mapping in _mappings)
            {
                if(mapping.Value.IsList)
                {
                    mapping.Value.Clear();
                }
            }
        }

        /// <summary>
        /// Overwrite this method to return the RDF classes of your resource type.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Class> GetTypes()
        {
            yield break;
        }

        /// <summary>
        /// Returns the uri with brackets. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Concat('<', Uri.OriginalString, '>');
        }

        /// <summary>
        /// A resource is at the moment deemed equal if the Uri is equal.
        /// </summary>
        /// <param name="comparand"></param>
        /// <returns></returns>
        public override bool Equals(object comparand)
        {
            IResource other = comparand as Resource;

            return other != null && other.Uri == Uri;
        }

        /// <summary>
        /// We return the hashcode of the original string of the uri
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Uri.OriginalString.GetHashCode();
        }

        /// <summary>
        /// Internal method to add the values. This is not public because the value is of type object and thus not typesafe for rdf.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal virtual void AddPropertyToMapping(Property property, object value, bool fromModel)
        {
            if (property.Uri.OriginalString == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
            {
                IResource v = value as IResource;

                if (GetTypes().Any(t => t.Uri == v.Uri))
                {
                    return;
                }
            }

            IPropertyMapping propertyMapping = GetPropertyMapping(property, value.GetType());

            if (propertyMapping != null)
            {
                // yes, so we try to set or add it
                if (fromModel && value is IResource)
                {
                    // we generate the resource from the model, so we cache all mapped resources
                    ResourceCache.CacheValue(propertyMapping, (value as IResource).Uri);
                }
                else
                {
                    propertyMapping.SetOrAddMappedValue(value);
                }
            }
            // no, we add it to the property
            else if (_properties.ContainsKey(property))
            {
                _properties[property].Add(value);
            }
            else
            {
                _properties.Add(property, new HashSet<object>() { value });
            }
        }

        /// <summary>
        /// Add a property with a resource as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public virtual void AddProperty(Property property, IResource value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a string as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, string value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a string and language as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, string value, CultureInfo language)
        {
            // TODO: 
            // Write a custom string class with an associated language.
            // Internally the language and string are stored as Tuple containing the string and culture info
            Tuple<string, string> aggregation = new Tuple<string, string>(value, language.Name.ToLower());

            AddPropertyToMapping(property, aggregation, false);
        }


        /// <summary>
        /// Add a property with a string and language as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, string value, string language)
        {
            // TODO: 
            // Write a custom string class with an associated language.
            // Internally the language and string are stored as Tuple containing the string and culture info
            Tuple<string, string> aggregation = new Tuple<string, string>(value, language.ToLower());

            AddPropertyToMapping(property, aggregation, false);
        }

        /// <summary>
        /// Add a property with a Int16 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, Int16 value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a Int32 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, Int32 value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a Int64 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, Int64 value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a UInt16 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, UInt16 value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a UInt32 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, UInt32 value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a UInt64 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, UInt64 value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a float as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, float value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a double as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, double value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a decimal as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, decimal value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a bool as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, bool value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a DateTime as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, DateTime value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Add a property with a TimeSpan as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, TimeSpan value)
        {
            throw new NotImplementedException();
            //TODO!
        }

        /// <summary>
        /// Add a property with a byte array as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, byte[] value)
        {
            AddPropertyToMapping(property, value, false);
        }

        /// <summary>
        /// Internal method to remove the values. This is not public because the value is of type object and thus not typesafe for rdf.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        internal virtual void RemovePropertyFromMapping(Property property, object value)
        {
            IPropertyMapping propertyMapping = GetPropertyMapping(property, value.GetType());

            if (propertyMapping != null)
            {
                propertyMapping.RemoveOrResetValue(value);
            }
            else if (_properties.ContainsKey(property))
            {
                _properties[property].Remove(value);

                if (_properties[property].Count == 0)
                {
                    _properties.Remove(property);
                }
            }
        }

        /// <summary>
        /// Removes a property with a IResource value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, IResource value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a string value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, string value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a string value associated with the given language.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, string value, CultureInfo language)
        {
            Tuple<string, string> aggregation = new Tuple<string, string>(value, language.Name.ToLower());

            RemovePropertyFromMapping(property, aggregation);
        }

        /// <summary>
        /// Removes a property with a string value associated with the given language.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, string value, string language)
        {
            Tuple<string, string> aggregation = new Tuple<string, string>(value, language.ToLower());

            RemovePropertyFromMapping(property, aggregation);
        }

        /// <summary>
        /// Removes a property with a Int16 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, Int16 value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a Int32 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, Int32 value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a Int64 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, Int64 value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a UInt16 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, UInt16 value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a UInt32 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, UInt32 value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a UInt64 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, UInt64 value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a float value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, float value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a double value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, double value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a decimal value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, decimal value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a bool value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, bool value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a DateTime value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, DateTime value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Removes a property with a byte array value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, byte[] value)
        {
            RemovePropertyFromMapping(property, value);
        }

        /// <summary>
        /// Internal method to remove all properties.
        /// </summary>
        internal virtual void ClearProperties()
        {
            _properties.Clear();

            // TODO: Clear all mapped properties.
        }

        /// <summary>
        /// Returns true if the resource has any object connected with the specified property.
        /// </summary>
        /// <param name="property">The property to be checked.</param>
        /// <returns>true if the property is associated, false if not</returns>
        public virtual bool HasProperty(Property property)
        {
            // NOTE: We do not need to check if the value list for the property contains
            // any values, since the property is removed from the dictionary if there
            // are no values left.
            bool result = _properties.ContainsKey(property);

            if (!result)
            {
                var mappings = from x in _mappings.Values where x.Property.Uri.Equals(property.Uri) && (!x.IsUnsetValue || ResourceCache.HasCachedValues(x)) select x;

                return mappings.Any();
            }

            return result;
        }

        /// <summary>
        /// Returns true if the specified value is connected to this resource with the given property.
        /// </summary>
        /// <param name="property">The property to be checked</param>
        /// <param name="value">The value that should be tested</param>
        /// <returns>true if the value is associated with the property, false if not</returns>
        public virtual bool HasProperty(Property property, object value)
        {
            bool result = false;

            if (_properties.ContainsKey(property))
            {
                if (value.GetType() == typeof(IResource))
                {
                    result = (_properties[property].Where(x => x.GetType() == typeof(IResource) && ((IResource)x).Uri.OriginalString == ((IResource)value).Uri.OriginalString).Count() > 0);
                }
                else
                {
                    result = (_properties[property].Contains(value));
                }
            }

            if (!result)
            {
                foreach (IPropertyMapping propertyMapping in _mappings.Values)
                {
                    if (propertyMapping.Property.Uri.Equals(property.Uri) && !propertyMapping.IsUnsetValue)
                    {
                        if (propertyMapping.GetValueObject().Equals(value) || (propertyMapping.IsList && (propertyMapping.GetValueObject() as IList).Contains(value)))
                        {
                            result = true;
                        }
                    }
                    else if (ResourceCache.HasCachedValues(propertyMapping) && value is IResource)
                    {
                        if (ResourceCache.HasCachedValues(propertyMapping, (value as IResource).Uri))
                        {
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the specified string value with the given language is connected to this resource with the given property.
        /// </summary>
        /// <param name="property">The property</param>
        /// <param name="value">The string value.</param>
        /// <param name="language">The language the string is in.</param>
        /// <returns></returns>
        public virtual bool HasProperty(Property property, string value, CultureInfo language)
        {
            Tuple<string, string> aggregation = new Tuple<string, string>(value, language.Name.ToLower());

            return HasProperty(property, aggregation);
        }

        /// <summary>
        /// Returns true if the specified string value with the given language is connected to this resource with the given property.
        /// </summary>
        /// <param name="property">The property</param>
        /// <param name="value">The string value.</param>
        /// <param name="language">The language the string is in.</param>
        /// <returns></returns>
        public virtual bool HasProperty(Property property, string value, string language)
        {
            Tuple<string, string> aggregation = new Tuple<string, string>(value, language);

            return HasProperty(property, aggregation);
        }

        /// <summary>
        /// This method lists all combinations of properties and values.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Tuple<Property, object>> ListValues()
        {
            foreach (var key in _properties)
            {
                foreach (object value in key.Value)
                {
                    yield return new Tuple<Property, object>(key.Key, value);
                }
            }

            foreach (IPropertyMapping propertyMapping in _mappings.Values)
            {
                if (!propertyMapping.IsUnsetValue)
                {
                    if (propertyMapping.IsList)
                    {
                        IList values = (IList)propertyMapping.GetValueObject();

                        foreach (object value in values)
                        {
                            yield return new Tuple<Property, object>(propertyMapping.Property, value);
                        }
                    }
                    else
                    {
                        yield return new Tuple<Property, object>(propertyMapping.Property, propertyMapping.GetValueObject());
                    }
                }
                else if (ResourceCache.HasCachedValues(propertyMapping))
                {
                    foreach (var resource in ResourceCache.ListCachedValues(propertyMapping))
                    {
                        yield return new Tuple<Property, object>(propertyMapping.Property, new Resource(resource));
                    }
                }
            }

            var types = GetTypes();

            if (types.Any())
            {
                Property rdfType;

                if (OntologyDiscovery.Properties.ContainsKey("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"))
                {
                    rdfType = OntologyDiscovery.Properties["http://www.w3.org/1999/02/22-rdf-syntax-ns#type"];
                }
                else
                {
                    rdfType = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
                }

                foreach (Class c in types)
                {
                    yield return new Tuple<Property, object>(rdfType, c);
                }
            }
        }

        /// <summary>
        /// Lists all values associated with one property.
        /// This inclues the mapped values as well.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual IEnumerable<object> ListValues(Property property)
        {
            // Add all values of the generic interface
            if (_properties.ContainsKey(property))
            {
                foreach (var value in _properties[property])
                    yield return value;
            }

            if (property.Uri.OriginalString == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
            {
#if NET_3_5
                foreach (object type in GetTypes().Cast<object>())
                    yield return type;
#else
                foreach (object type in GetTypes())
                    yield return type;
#endif

                // We do not need to add mapped values for the RDF type property.
                yield break;
            }

            // iterate over the mapping and add all values
            foreach (IPropertyMapping propertyMapping in _mappings.Values.Where(p => p.Property.Equals(property)))
            {
                if (!propertyMapping.IsUnsetValue)
                {
                    if (propertyMapping.IsList)
                    {
                        IList value = (IList)propertyMapping.GetValueObject();
                        if (!string.IsNullOrEmpty(Language) && !propertyMapping.LanguageInvariant && propertyMapping.GenericType == typeof(string))
                        {
                            foreach (var x in value)
                                yield return new Tuple<string, string>(x as string, Language);
                        }
                        else
                        {
                            foreach (object v in value.Cast<object>())
                                yield return v;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Language) && !propertyMapping.LanguageInvariant && propertyMapping.DataType == typeof(string))
                        {
                            yield return new Tuple<string, string>(propertyMapping.GetValueObject() as string, Language);
                        }
                        else
                        {
                            yield return propertyMapping.GetValueObject();
                        }
                    }
                }
                else if (ResourceCache.HasCachedValues(propertyMapping))
                {
                    foreach (var r in ResourceCache.ListCachedValues(propertyMapping))
                    {
                        yield return new Resource(r);
                    }
                }
            }
        }

        /// <summary>
        /// List all available properties.
        /// This includes mapped properties if they have valid values.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Property> ListProperties()
        {
            HashSet<Property> properties = new HashSet<Property>();

            if (GetTypes().Any())
            {
                Property rdfType = OntologyDiscovery.Properties["http://www.w3.org/1999/02/22-rdf-syntax-ns#type"];

                properties.Add(rdfType);
            }

            // List all properties of the generic interface which have values
            foreach (var keyValue in _properties)
            {
                if (keyValue.Value.Count > 0)
                {
                    properties.Add(keyValue.Key);
                }
            }

            // List all mapped properties which have values (Listtypes) or have been set
            foreach (IPropertyMapping mappingObject in _mappings.Values)
            {
                if (!mappingObject.IsUnsetValue)
                {
                    properties.Add(mappingObject.Property);
                }
            }

            return properties;
        }

        /// <summary>
        /// Return the value for a given property.
        /// </summary>
        /// <param name="property">A RDF property.</param>
        /// <returns>The value on success, <c>null</c> if the object has no such property.</returns>
        public virtual object GetValue(Property property)
        {
            return GetValue(property, null);
        }

        /// <summary>
        /// Return the value for a given property with a predefined default value.
        /// </summary>
        /// <param name="property">A RDF property.</param>
        /// <returns>The value on success, the default value if the object has no such property.</returns>
        public object GetValue(Property property, object defaultValue)
        {
            var result = ListValues(property).ToList();

            return result.Count > 0 ? result.First() : defaultValue;
        }

        /// <summary>
        /// Persist changes in the model.
        /// </summary>
        public virtual void Commit()
        {
            if (_model != null && IsReadOnly == false)
            {
                // Update Resource in Model
                _model.UpdateResource(this);
            }
        }

        /// <summary>
        /// Reload the resource from the model.
        /// </summary>
        public void Rollback()
        {
            using (Resource resource = Model.GetResource(Uri, GetType()) as Resource)
            {
                _model = resource._model;
                _properties = resource._properties;

                Uri = resource.Uri;

                IsNew = resource.IsNew;
                IsSynchronized = resource.IsSynchronized;

                ResourceCache.Clear();

                foreach (KeyValuePair<string, IPropertyMapping> mapping in _mappings)
                {
                    if (!resource._mappings.ContainsKey(mapping.Key))
                    {
                        continue;
                    }

                    IPropertyMapping persistedMapping = resource._mappings[mapping.Key];

                    mapping.Value.CloneFrom(persistedMapping);

                    if (resource.ResourceCache.HasCachedValues(persistedMapping))
                    {
                        ResourceCache.CacheValues(mapping.Value, resource.ResourceCache.ListCachedValues(persistedMapping));
                    }
                }

                foreach (string name in _notifyingProperties)
                {
                    RaisePropertyChanged(name);
                }
            }

            // NOTE: We do not need to copy the classes, we have to assume the mapped type stays the same.
        }

        /// <summary>
        /// This method returns the mapped property of the given rdf property and type. It returns null if this mapping is not available.
        /// </summary>
        /// <param name="property">Rdf property to be tested.</param>
        /// <param name="type">Type of the mapping.</param>
        /// <returns></returns>
        private IPropertyMapping GetPropertyMapping(Property property, Type type)
        {
            foreach (IPropertyMapping mappingObject in _mappings.Values)
            {
                if (mappingObject.Property.Uri.OriginalString == property.Uri.OriginalString && mappingObject.IsTypeCompatible(type))
                {
                    return mappingObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the value from the mapped property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyMapping"></param>
        /// <returns></returns>
        protected virtual T GetValue<T>(PropertyMapping<T> propertyMapping)
        {
            LoadCachedValues(propertyMapping);

            return propertyMapping.GetValue();
        }

        /// <summary>
        /// Set the mapped value. This also raises the PropertyChanged event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyMapping"></param>
        /// <param name="value"></param>
        protected virtual void SetValue<T>(PropertyMapping<T> propertyMapping, T value)
        {
            propertyMapping.SetValue(value);
        }

        /// <summary>
        /// Load all cached resources from the mapped property. The values of the mapped property are resolved when this method returns.
        /// </summary>
        /// <param name="propertyMapping"></param>
        private void LoadCachedValues(IPropertyMapping propertyMapping)
        {
            if (ResourceCache.HasCachedValues(propertyMapping))
            {
                ResourceCache.LoadCachedValues(propertyMapping);
            }
        }

        /// <summary>
        /// Register a property name to raise the INotifyProperty signal on rollback.
        /// </summary>
        /// <param name="propertyName">Name of a property.</param>
        protected void RegisterPropertyChanged(string propertyName)
        {
            _notifyingProperties.Add(propertyName);
        }

        /// <summary>
        /// Raises the PropertyChanged event of the object.
        /// </summary>
        /// <param name="propertyName">Name of a property.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void ReloadLocalizedMappings()
        {
            foreach (var mapping in _mappings.Where(x => (x.Value.DataType == typeof(string) || x.Value.GenericType == typeof(string)) && !x.Value.LanguageInvariant))
            {
                if (!mapping.Value.IsUnsetValue)
                {
                    TransferMappingToProperties(mapping.Value);
                    mapping.Value.Clear();
                }
                mapping.Value.Language = Language;
                foreach (var value in ListValues(mapping.Value.Property).ToList())
                {
                    if (string.IsNullOrEmpty(Language))
                    {
                        if (value is string)
                        {
                            mapping.Value.SetOrAddMappedValue(value);
                            _properties[mapping.Value.Property].Remove(value);
                        }

                    }
                    else
                    {
                        if (value is Tuple<string, string>)
                        {
                            var localizedString = value as Tuple<string, string>;
                            if (string.Compare(localizedString.Item2, Language, true) == 0)
                            {
                                mapping.Value.SetOrAddMappedValue(localizedString.Item1);
                                _properties[mapping.Value.Property].Remove(localizedString);
                            }
                        }
                    }

                }
            }
        }

        private void TransferMappingToProperties(IPropertyMapping mapping)
        {
            if (!_properties.ContainsKey(mapping.Property))
            {
                _properties.Add(mapping.Property, new HashSet<object>());
            }

            if (mapping.IsList)
            {
                foreach (var x in mapping.GetValueObject() as IList)
                    _properties[mapping.Property].Add(x);
            }
            else
            {
                _properties[mapping.Property].Add(mapping.GetValueObject());
            }

        }

        /// <summary>
        /// Dispose this resource. 
        /// Does nothing meaningful currently.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                GC.SuppressFinalize(this);

                IsDisposed = true;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Needed for the implementation of the INotifyPropertyChanged interface.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Debugging

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (ThrowOnInvalidPropertyName)
                {
                    throw new Exception(msg);
                }
                else
                {
                    Debug.Fail(msg);
                }
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion
    }
}
