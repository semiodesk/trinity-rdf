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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Semiodesk.Trinity.Store;
#if !NET_3_5
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
#endif

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
            {"virtuoso", new VirtuosoStoreProvider()},
            {"dotnetrdf", new dotNetRDFStoreProvider()},
            {"sparqlendpoint", new SparqlEndpointStoreProvider()}
        };

        #region Factory Methods

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

        public static IStore CreateStore(string connectionString)
        {
            var config = ParseConfiguration(connectionString);
            if (config.ContainsKey("provider"))
            {
                string provider = config["provider"];
                if (_storeConfigurations.ContainsKey(provider))
                {
                    StoreProvider p = _storeConfigurations[provider];
                    return p.GetStore(config);
                }
            }
            return null;
        }

        public static IStore CreateStoreFromConfiguration(string name = null)
        {
            foreach( ConnectionStringSettings setting in ConfigurationManager.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(name) && setting.Name != name)
                    continue;
                            
                string conString = setting.ConnectionString;
                if (setting.ProviderName == "Semiodesk.Trinity" && StoreFactory.TestConnectionString(conString))
                    return CreateStore(conString);
            }
            return null;
        }

        public static bool LoadProvider(string assemblyPath)
        {
            bool result = false;
            try
            {
            #if !NET_3_5
                AssemblyCatalog catalog = new AssemblyCatalog(assemblyPath);
                var container = new CompositionContainer(catalog);
                foreach (var item in container.GetExports<StoreProvider>())
                {
                    StoreProvider provider = item.Value;
                    if (!_storeConfigurations.ContainsKey(provider.Name))
                    {
                        _storeConfigurations.Add(provider.Name, provider);
                        result = true;
                    }
                }
            #else
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                var types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(StoreProvider)));
                foreach (var item in types)
                {
                    StoreProvider provider = (StoreProvider)Activator.CreateInstance(item);
                    if (!_storeConfigurations.ContainsKey(provider.Name))
                    {
                        _storeConfigurations.Add(provider.Name, provider);
                        result = true;
                    }
                }
            #endif
            }
            catch (Exception)
            {
                return false;
            }
            return result;
        }

        public static bool LoadProvider(FileInfo assembly)
        {
            return LoadProvider(assembly.FullName);
        }

    #endregion
    }
}
