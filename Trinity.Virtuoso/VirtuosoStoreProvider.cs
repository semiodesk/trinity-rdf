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
// Copyright (c) Semiodesk GmbH 2018


using System.Collections.Generic;
using System.Composition;


namespace Semiodesk.Trinity.Store.Virtuoso
{

    [Export(typeof(StoreProvider))]

    public class VirtuosoStoreProvider : StoreProvider
    {

        public VirtuosoStoreProvider()
        {
            Name = "virtuoso";
        }

        public override IStore GetStore(Dictionary<string, string> configurationDictionary )
        {
            string hostKey = "host";
            string host = "127.0.0.1";
                          
            if(configurationDictionary.ContainsKey(hostKey))
                host = configurationDictionary[hostKey];


            string portKey = "port";
            int port;
            if (!configurationDictionary.ContainsKey(portKey) ||!int.TryParse(configurationDictionary[portKey], out port))
            {
                #if !DEBUG 
                port = 1112;
                #else
                port = 1111;
                #endif
            }

            string userKey = "uid";
            string user = "dba";
            if(configurationDictionary.ContainsKey(userKey))
                 user= configurationDictionary["uid"];


            string passwordKey = "pw";
            string password = "dba";
            if (configurationDictionary.ContainsKey(passwordKey))
                password = configurationDictionary[passwordKey];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
               return null;

            string inferenceRule = null;
            string ruleKey = "rule";
            if (configurationDictionary.ContainsKey(ruleKey))
            {
                inferenceRule = configurationDictionary[ruleKey];
            }


            VirtuosoStore store = new VirtuosoStore(host, port, user, password, inferenceRule);
            return store;
        }

    }
}
