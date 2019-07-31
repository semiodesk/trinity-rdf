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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Semiodesk.Trinity.Serialization
{
    /// <summary>
    /// Converts resources to and from JSON format.
    /// </summary>
    public class JsonResourceConverter : JsonConverter
    {
        #region Members

        private IStore _store;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new instance of the <c>JsonResourceConverter</c> class.
        /// </summary>
        /// <param name="store">A triple store.</param>
        public JsonResourceConverter(IStore store)
        {
            _store = store;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates if the given object can be converted.
        /// </summary>
        /// <param name="objectType">An object.</param>
        /// <returns><c>true</c> if the object is of type <c>Resource</c>, <c>false</c> otherwise.</returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Resource).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Convert a JSON string into an object.
        /// </summary>
        /// <param name="reader">A JSON reader.</param>
        /// <param name="objectType">Returned object type.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject resourceJson = JObject.Load(reader);
            JObject modelJson = resourceJson["Model"].Value<JObject>();

            if (modelJson == null)
            {
                return null;
            }

            Uri modelUri = new Uri(modelJson["Uri"].Value<string>());
            Uri resourceUri = new Uri(resourceJson["Uri"].Value<string>());

            IModel model = _store.GetModel(modelUri);

            // We could load the resource from the model here, however this would
            // initialize all the list-type properties of the resource. This is a problem
            // since the JSON serializer does not clear the lists and simply adds the new values.
            Resource resource = model.GetResource<Resource>(resourceUri);

            if (resource == null)
            {
                return null;
            }

            // Set the model so the resource can be committed.
            resource.Model = model;

            // Clear the lists to prevent the serializer to add duplicate entries.
            resource.ClearListPropertyMappings();

            // Do not let the serializer set the model or URI..
            resourceJson.Remove("Model");
            resourceJson.Remove("Uri");

            // Load all the other properties from the JSON.
            serializer.Populate(resourceJson.CreateReader(), resource);

            return resource;
        }

        /// <summary>
        /// Indicates if the converter can write JSON.
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Write the JSON representation of an object.
        /// </summary>
        /// <param name="writer">The JSON writer to be used.</param>
        /// <param name="value">The object value.</param>
        /// <param name="serializer">The JSON serializer to be used.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
