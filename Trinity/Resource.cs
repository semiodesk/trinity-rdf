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
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Collections;
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
        /// All classes as discovered by InitialiseClassMapping.
        /// </summary>
        public readonly List<Class> Classes = new List<Class>();

        /// <summary>
        /// The cache for the associated resources, needed to support lazy loading for mapping.
        /// </summary>
        internal ResourceCache ResourceCache;

        /// <summary>
        /// This dictionary contains the properties and the associated values.
        /// </summary>
        private Dictionary<Property, List<object>> _properties;

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
        public bool IsNew { get; set; }

        /// <summary>
        /// Indicates if the resources has been disposed.
        /// </summary>
        protected bool IsDisposed;

        /// <summary>
        /// True if the properties of the resources has been committed to the model.
        /// </summary>
        [Browsable(false)]
        public bool IsSynchronized { get; set; }

        public bool IsReadOnly { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Private since a Resource cannot be created without a URI.
        /// </summary>
        private Resource()
        {}

        /// <summary>
        /// Create a new resource with a given Uri.
        /// </summary>
        /// <param name="uri"></param>
        public Resource(UriRef uri)
        {
            Initialize(uri);
        }

        public Resource(Uri uri)
        {
            Initialize(uri.ToUriRef());
        }

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
            Classes = other.Classes;
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
            _properties = new Dictionary<Property, List<object>>();
            _mappings = new Dictionary<string, IPropertyMapping>();

            ResourceCache = new ResourceCache();

            Uri = uri;

            InitialisePropertyMapping();
            InitialiseClassMapping();

            IsSynchronized = false;
            IsNew = true;

            // We cannot register the resource with the model, if no model is set.
        }

        /// <summary>
        /// Overwrite this method to return the RDF classes of your resource type.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Class> GetTypes()
        {
            return new Class[] {};
        }

        /// <summary>
        /// Returns the uri with brackets. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("<{0}>",Uri.OriginalString);
        }

        /// <summary>
        /// A resource is at the moment deemed equal if the Uri is equal.
        /// </summary>
        /// <param name="comparand"></param>
        /// <returns></returns>
        public override bool Equals(object comparand)
        {
            return (comparand is Resource) ? Uri == (comparand as Resource).Uri : false;
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
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void AddProperty(Property property, object value, bool fromModel = false)
        {
            if (property.Uri.OriginalString == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
            {
                IResource v = (IResource)value;
                foreach (Class item in Classes)
                {
                    if (item.Uri == v.Uri)
                        return;
                }
            }

            IPropertyMapping propertyMapping = GetPropertyMapping(property, value.GetType());

            // Is property mapped
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
                return;
            }
            else
            {
                // no, we add it to the property
                if (_properties.ContainsKey(property))
                {
                    if (!_properties[property].Contains(value))
                        _properties[property].Add(value);
                }
                else
                {
                    List<object> l = new List<object>();
                    l.Add(value);
                    _properties.Add(property, l);
                }

            }
        }

        /// <summary>
        /// Add a property with a resource as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public virtual void AddProperty(Property property, IResource value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a string as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, String value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a string and language as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, String value, CultureInfo language)
        {
            // TODO: 
            // Write a custom string class with an associated language.
            // Internally the language and string are stored as Tuple containing the string and culture info
            Tuple<string, CultureInfo> aggregation = new Tuple<string, CultureInfo>(value, language);
            AddProperty(property, (object)aggregation);
        }

        /// <summary>
        /// Add a property with a Int16 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, Int16 value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a Int32 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, Int32 value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a Int64 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, Int64 value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a UInt16 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, UInt16 value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a UInt32 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, UInt32 value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a UInt64 as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, UInt64 value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a float as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, float value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a double as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, double value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a bool as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, bool value)
        {
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Add a property with a DateTime as value.
        /// If this property is mapped with a compatible type, it will be filled with the given value.
        /// </summary>
        public void AddProperty(Property property, DateTime value)
        {
            AddProperty(property, (object)value);
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
            AddProperty(property, (object)value);
        }

        /// <summary>
        /// Internal method to remove the values. This is not public because the value is of type object and thus not typesafe for rdf.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        internal virtual void RemoveProperty(Property property, object value)
        {
            IPropertyMapping propertyMapping = GetPropertyMapping(property, value.GetType());

            if (propertyMapping != null)
            {
                propertyMapping.RemoveOrResetValue(value);
                return;
            }
            else
            {
                if (_properties.ContainsKey(property))
                {
                    _properties[property].Remove(value);

                    if (_properties[property].Count == 0)
                    {
                        _properties.Remove(property);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a property with a IResource value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, IResource value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a string value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, string value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a string value associated with the given language.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, string value, CultureInfo language)
        {
            Tuple<string, CultureInfo> aggregation = new Tuple<string, CultureInfo>(value, language);

            RemoveProperty(property, aggregation);
        }

        /// <summary>
        /// Removes a property with a Int16 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, Int16 value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a Int32 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, Int32 value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a Int64 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, Int64 value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a UInt16 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, UInt16 value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a UInt32 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, UInt32 value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a UInt64 value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, UInt64 value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a float value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, float value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a double value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, double value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a bool value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, bool value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a DateTime value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, DateTime value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Removes a property with a byte array value.
        /// If this property is mapped with a compatible type, the given value will be removed.
        /// </summary>
        public void RemoveProperty(Property property, byte[] value)
        {
            RemoveProperty(property, (object)value);
        }

        /// <summary>
        /// Internal method to remove all properties.
        /// </summary>
        internal virtual void ClearProperties()
        {
            _properties.Clear();
            //TODO: clear all mapped properties
        }

        /// <summary>
        /// Returns true if the resource has any object connected with the specified property.
        /// </summary>
        /// <param name="property">The property to be checked.</param>
        /// <returns>true if the property is associated, false if not</returns>
        public virtual bool HasProperty(Property property)
        {
            bool result = false;
            result = (_properties.ContainsKey(property) && _properties[property].Count > 0);
            if (!result)
            {
                var mappings = from x in _mappings.Values where x.RdfProperty.Uri.Equals(property.Uri) && ( !x.IsUnsetValue || ResourceCache.HasCachedValues(x) ) select x;
                if (mappings.Count() > 0)
                {
                    result = true;
                }
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
            if( _properties.ContainsKey(property) )
            {
                if (value.GetType() == typeof(IResource))
                {
                    result = (_properties[property].Where( x => x.GetType() == typeof(IResource) && ((IResource)x ).Uri.OriginalString == ((IResource)value).Uri.OriginalString).Count() > 0);
                }
                else
                    result = (_properties[property].Contains(value));
            }
            if (!result)
            {
                foreach (IPropertyMapping propertyMapping in _mappings.Values)
                {
                    if (propertyMapping.RdfProperty.Uri.Equals(property.Uri) && !propertyMapping.IsUnsetValue )
                    {
                        if (propertyMapping.GetValueObject().Equals(value) || (propertyMapping.IsList && (propertyMapping.GetValueObject() as IList).Contains(value)))
                        {
                            result = true;
                        }
                    }
                    else if (ResourceCache.HasCachedValues(propertyMapping) && value is IResource)
                    {
                        if (ResourceCache.HasCachedValues(propertyMapping, (value as IResource).Uri))
                            result = false;
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
            Tuple<string, CultureInfo> aggregation = new Tuple<string, CultureInfo>(value, language);
            return HasProperty(property, aggregation);
        }

        /// <summary>
        /// This method lists all combinations of properties and values.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Tuple<Property, object>> ListValues()
        {
            foreach (var keyValue in _properties)
            {
                foreach (object val in keyValue.Value)
                    yield return new Tuple<Property, object>(keyValue.Key, val);
            }

            foreach (IPropertyMapping propertyMapping in _mappings.Values)
            {
                if (!propertyMapping.IsUnsetValue )
                {
                    if (propertyMapping.IsList)
                    {
                        IList values = (IList)propertyMapping.GetValueObject();
                        foreach (object val in values)
                            yield return new Tuple<Property, object>(propertyMapping.RdfProperty, val);
                    }
                    else
                    {
                        yield return new Tuple<Property, object>(propertyMapping.RdfProperty, propertyMapping.GetValueObject());
                    }
                }
                else if (ResourceCache.HasCachedValues(propertyMapping))
                {
                    foreach (var r in ResourceCache.ListCachedValues(propertyMapping))
                        yield return new Tuple<Property, object>(propertyMapping.RdfProperty, new Resource(r));

                }
            }

            if (Classes.Count > 0)
            {
                Property rdfType;
                if( OntologyDiscovery.Properties.ContainsKey("http://www.w3.org/1999/02/22-rdf-syntax-ns#type") )
                    rdfType = OntologyDiscovery.Properties["http://www.w3.org/1999/02/22-rdf-syntax-ns#type"];
                else
                    rdfType = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
                foreach (Class c in Classes)
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
        public virtual List<object> ListValues(Property property)
        {
            List<object> valueList = new List<object>();

            // Add all values of the generic interface
            if (_properties.ContainsKey(property))
            {
                valueList.AddRange(_properties[property]);
            }

            // iterate over the mapping and add all values
            foreach (IPropertyMapping propertyMapping in _mappings.Values)
            {
                if(propertyMapping.RdfProperty.Equals(property))
                {
                    if (!propertyMapping.IsUnsetValue)
                    {
                        if (propertyMapping.IsList)
                        {
                            IList value = (IList) propertyMapping.GetValueObject();
                            valueList.AddRange(value.Cast<object>());
                        }
                        else
                        {
                            valueList.Add(propertyMapping.GetValueObject());
                        }
                    }
                else
                    if (ResourceCache.HasCachedValues(propertyMapping))
                    {
                        foreach (var r in ResourceCache.ListCachedValues(propertyMapping))
                            valueList.Add(new Resource(r) as object);

                    }
                }
            }

            if (property.Uri.OriginalString == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
            {
                foreach (var type in Classes)
                {
                    valueList.Add(type);
                }
                
            }

            return valueList;
        }

        /// <summary>
        /// List all available properties.
        /// This includes mapped properties if they have valid values.
        /// </summary>
        /// <returns></returns>
        public virtual List<Property> ListProperties()
        {
            List<Property> propertyList = new List<Property>();

            if (Classes.Count > 0)
            {
                Property rdfType = OntologyDiscovery.Properties["http://www.w3.org/1999/02/22-rdf-syntax-ns#type"];
                propertyList.Add(rdfType);
            }

            // List all properties of the generic interface which have values
            foreach (var keyValue in _properties)
            {
                if (!propertyList.Contains(keyValue.Key) && keyValue.Value.Count > 0)
                    propertyList.Add(keyValue.Key);
            }

            // List all mapped properties which have values (Listtypes) or have been set
            foreach (IPropertyMapping mappingObject in _mappings.Values)
            {
                if (!propertyList.Contains(mappingObject.RdfProperty) && !mappingObject.IsUnsetValue)
                {
                    propertyList.Add(mappingObject.RdfProperty);
                }
            }

            return propertyList;
        }

        public virtual object GetValue(Property property)
        {
            return GetValue(property, null);
        }

        public object GetValue(Property property, object defaultValue)
        {
            List<object> result = ListValues(property);

            if (result.Count > 0)
            {
                return ListValues(property).First();
           }
            else
            {
                return defaultValue;
            }
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
            Resource other = (Resource)Model.GetResource(this.Uri);
            _model = other._model;
            ResourceCache.Clear();
            _properties = other._properties;

            foreach (var x in _mappings)
            {
                var otherMapping = other._mappings[x.Key];
                x.Value.CloneFrom(otherMapping);
                if (other.ResourceCache.HasCachedValues(otherMapping))
                {
                    ResourceCache.CacheValues(x.Value, other.ResourceCache.ListCachedValues(otherMapping));
                }
            }

            Uri = other.Uri;

            IsNew = other.IsNew;
            IsSynchronized = other.IsSynchronized;

            other.Dispose();

            foreach (IPropertyMapping mapping in _mappings.Values)
            {
                RaisePropertyChanged(mapping.PropertyName);
                foreach (string relatedProperty in mapping.RelatedProperties)
                    RaisePropertyChanged(relatedProperty);
            }
            // We do not need to copy the classes, we have to assume the mapped type stays the same.
        }

        #region Mapping Functionality

        /// <summary>
        /// Loads and initialises all mapped properties. 
        /// </summary>
        protected void InitialisePropertyMapping()
        {
            Type thisType = GetType();

            FieldInfo[] b = thisType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            IEnumerable<object> mappingList = from n in b where (n.FieldType.GetInterface("IPropertyMapping") != null) select n.GetValue(this);

            foreach (IPropertyMapping mappingObject in mappingList)
            {
                _mappings.Add(mappingObject.PropertyName, mappingObject);
            }
        }

        /// <summary>
        /// Loads the rdf types of this resource.
        /// </summary>
        protected void InitialiseClassMapping()
        {
            Classes.AddRange(GetTypes());
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
                if (mappingObject.RdfProperty.Uri.OriginalString == property.Uri.OriginalString && mappingObject.IsTypeCompatible(type))
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

            RaisePropertyChanged(propertyMapping.PropertyName);
            foreach (string relatedProperty in (propertyMapping as IPropertyMapping).RelatedProperties)
                RaisePropertyChanged(relatedProperty);
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

        #endregion

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

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

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
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
