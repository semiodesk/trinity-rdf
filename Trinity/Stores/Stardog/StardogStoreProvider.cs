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

using System.Collections.Generic;
using System.IO;
using VDS.RDF.Storage.Management;
using Semiodesk.Trinity.Store.Stardog;

#if NETSTANDARD2_0
using System.Composition;
#elif !NET35
using System.ComponentModel.Composition;
#endif

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// This class allows the usage of the Stardog store.
    /// An IStore handle can be created by calling
    /// StoreFactory.CreateStore("provider=stardog;host=http://localhost:5820;uid=admin;pw=admin;sid=MyStore");
    /// </summary>
    #if ! NET35
    [Export(typeof(StoreProvider))]
    #endif
    public class StardogStoreProvider : StoreProvider
    {
        #region Constructor

        public StardogStoreProvider()
        {
            Name = "stardog";
        }

        #endregion

        public override IStore GetStore(Dictionary<string, string> configurationDictionary)
        {
            string hostKey = "host";
            string host = "http://localhost:5820";
            if (configurationDictionary.ContainsKey(hostKey))
                host = configurationDictionary[hostKey];

            string userKey = "uid";
            string user = "dba";
            if (configurationDictionary.ContainsKey(userKey))
                user = configurationDictionary["uid"];

            string passwordKey = "pw";
            string password = "dba";
            if (configurationDictionary.ContainsKey(passwordKey))
                password = configurationDictionary[passwordKey];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
                return null;

            string storeIdKey = "sid";
            string storeId = null;
            if (configurationDictionary.ContainsKey(storeIdKey))
            {
                storeId = configurationDictionary[storeIdKey];
            }

            return new StardogStore(host, user, password, storeId);
        }

    }
}
