﻿// LICENSE:
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
// Copyright (c) Semiodesk GmbH 2023

using System.Collections.Generic;
using System.Composition;

namespace Semiodesk.Trinity.Store.GraphDB
{
    [Export(typeof(StoreProvider))]
    public class GraphDBStoreProvider : StoreProvider
    {
        public GraphDBStoreProvider()
        {
            Name = "graphdb";
        }

        private string GetValue(Dictionary<string, string> config, string key, string defaultValue = null)
        {
            return config.ContainsKey(key) ? config[key] : defaultValue;
        }
        
        public override IStore GetStore(Dictionary<string, string> config)
        {
            var hostUri = GetValue(config, "host", "http://localhost:7200/");
            var repositoryId = GetValue(config, "repository");
            var user = GetValue(config, "uid");
            var password = GetValue(config, "pw");

            if (string.IsNullOrEmpty(hostUri))
            {
                return null;
            }
            
            return new GraphDBStore(hostUri, repositoryId, user, password);
        }
    }
}
