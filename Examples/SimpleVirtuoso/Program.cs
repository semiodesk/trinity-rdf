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

using Semiodesk.Trinity;
using Semiodesk.Trinity.Store.Virtuoso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleVirtuoso
{
    class Program
    {
        /// <summary>
        /// The model we are working on
        /// </summary>
        static IModel Model { get; set; }

        static void Main()
        {
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterCallingAssembly();

            // Load the virtuoso store provider
            StoreFactory.LoadProvider<VirtuosoStoreProvider>();

            // Connect to the virtuoso store
            IStore store = StoreFactory.CreateStore("provider=virtuoso;host=127.0.0.1;port=1111;uid=dba;pw=dba;rule=urn:example/ruleset");
            store.InitializeFromConfiguration(Path.Combine(Environment.CurrentDirectory, "ontologies.config"));

            // Uncomment to log all executed queries to the console.
            // store.Log = (query) => Console.WriteLine(query);

            // A model is where we collect resources logically belonging together
            Model = store.GetModel(new Uri("http://example.com/model"));
            Model.Clear();

            Drug ibu = Model.CreateResource<Drug>(new Uri("https://www.hexal.de/patienten/produkte/IbuHEXAL-akut"));
            ibu.ActiveIngredient = "Ibuprophene";
            ibu.ProprietaryName = "IbuHEXAL akut";
            ibu.Commit();


            Patient john = Model.CreateResource<Patient>(new Uri("http://example.com/patient/john"));
            john.FirstName = "John";
            john.LastName = "Doe";
            john.BirthDate = new DateTime(2000, 1, 1);
            john.Drugs.Add(ibu);
            john.Commit();

            Person alice = Model.CreateResource<Person>(new Uri("http://example.com/person/alice"));
            alice.FirstName = "Alice";
            alice.LastName = "Doe";
            alice.BirthDate = new DateTime(2000, 1, 1);
            alice.Commit();


            var theDoeFamily = from person in Model.AsQueryable<Person>(true) where person.LastName.StartsWith("d", StringComparison.InvariantCultureIgnoreCase) select person;
            foreach (var p in theDoeFamily)
            {
                Console.WriteLine($"Name: {p.FirstName} {p.LastName} Birthdate: {p.BirthDate}");

            }
                



            Console.WriteLine();
            Console.WriteLine("Press ANY key to close..");
            Console.ReadLine();
        }
    }
}
