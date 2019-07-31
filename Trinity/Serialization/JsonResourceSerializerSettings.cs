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

namespace Semiodesk.Trinity.Serialization
{
    /// <summary>
    /// Settings for the serializing resources to and from JSON format.
    /// </summary>
    public class JsonResourceSerializerSettings : JsonSerializerSettings
    {
        #region Constructors

        /// <summary>
        /// Create a new instance of the <c>JsonResourceSerializerSettings</c> class.
        /// </summary>
        /// <param name="store">A triple store.</param>
        public JsonResourceSerializerSettings(IStore store)
        {
            // Allow to use the private parameterless constructor of the Resource class.
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;

            // A custom conveter for loading the URI and setting the model.
            Converters = new JsonConverter[] { new JsonResourceConverter(store) };
        }

        #endregion
    }
}
