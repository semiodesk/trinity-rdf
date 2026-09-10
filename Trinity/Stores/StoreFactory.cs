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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Semiodesk.Trinity.Store;
using System.Net;


namespace Semiodesk.Trinity
{
    /// <summary>
    /// This is the factory for object implementing the IStore interface.
    /// If you want to use your own store, you can load the assembly containing the provider with the LoadProvider method.
    /// </summary>
    public class StoreFactory
    {
        private static readonly Dictionary<string, StoreProvider> _storeConfigurations = new Dictionary<string, StoreProvider>()
        {
            {"dotnetrdf", new dotNetRDFStoreProvider()},
            {"sparqlendpoint", new SparqlEndpointStoreProvider()}
        };

        #region Factory Methods

        /// <summary>
        /// Tests if the given connection string is valid.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static bool TestConnectionString(string connectionString)
        {
            bool res = false;
            var config = ParseConfiguration(connectionString);
            if (config.ContainsKey("provider"))
            {
                string provider = config["provider"];
                res = _storeConfigurations.ContainsKey(provider);
            }
            return res;
        }

        internal static Dictionary<string, string> ParseConfiguration(string configurationString)
        {
            Regex r = new Regex("(?<name>.*?)=(?<value>.*?)(;|$)");

            return r.Matches(configurationString).Cast<Match>().ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value);
        }

        /// <summary>
        /// Creates a store from the given connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static IStore CreateStore(string connectionString)
        {
            var config = ParseConfiguration(connectionString);

            if (config.ContainsKey("provider"))
            {
                string provider = config["provider"];

                if (_storeConfigurations.ContainsKey(provider))
                {
                    try
                    {
                        StoreProvider p = _storeConfigurations[provider];
                        return p.GetStore(config);
                    }
                    catch (Exception e)
                    {
                        throw new StoreProviderMissingException($"An error occured while trying to create the store with key '{provider}'. Check the inner exception.", e);
                    }
                }

                throw new StoreProviderMissingException($"No store provider with key '{provider}' found. Try to register it with 'StoreFactory.LoadProvider()'");
            }

            throw new StoreProviderMissingException("No store provider key specified.");
        }

        /// <summary>
        /// Creates a temporary in-memory store using the dotNetRDF provider.
        /// </summary>
        /// <returns></returns>
        public static IStore CreateMemoryStore()
        {
            return CreateStore("provider=dotnetrdf");
        }


        /// <summary>
        /// Create a store suitable for querying SPARQL protocol endpoints.
        /// </summary>
        /// <param name="url">URL of the SPARQL endpoint.</param>
        /// <param name="proxy">Proxy settings to use for the HTTP connection.</param>
        /// <param name="credentials">Network credentials</param>
        /// <returns></returns>
        public static IStore CreateSparqlEndpointStore(Uri url, IWebProxy proxy = null, NetworkCredential credentials = null)
        {
            return new SparqlEndpointStore(url, proxy, credentials);
        }

        /// <summary>
        /// Loads a store provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static bool LoadProvider(StoreProvider provider)
        {
            bool result = false;

            if (!_storeConfigurations.ContainsKey(provider.Name))
            {
                _storeConfigurations.Add(provider.Name, provider);
                result = true;
            }


            return result;
        }

        /// <summary>
        /// Loads a store provider
        /// </summary>
        /// <typeparam name="T">A store provider type</typeparam>
        /// <returns></returns>
        public static bool LoadProvider<T>() where T : StoreProvider, new()
        {
            var provider = new T();
            bool result = false;

            if (!_storeConfigurations.ContainsKey(provider.Name))
            {
                _storeConfigurations.Add(provider.Name, provider);
                result = true;
            }


            return result;
        }



        #endregion
    }
}
