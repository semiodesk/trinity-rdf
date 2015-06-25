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
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Diagnostics;
using System.Reflection;
using Semiodesk.Trinity.Ontologies;
using System.Collections.ObjectModel;
using System.Threading;

namespace Semiodesk.Trinity.Tests
{
    class ContactList : Resource
    {
         #region Constructors
        public ContactList(Uri uri) : base(uri) { }
        #endregion

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { nco.ContactList };
        }

        protected PropertyMapping<ObservableCollection<Contact>> containsContactProperty = new PropertyMapping<ObservableCollection<Contact>>("ContainsContact", nco.containsContact, new ObservableCollection<Contact>());
        
        public ObservableCollection<Contact> ContainsContact
        {
            get { return GetValue(containsContactProperty); }
            set { SetValue(containsContactProperty, value); }
        }
    }

    class EmailAddress : Resource
    {
        #region Constructors
        public EmailAddress(Uri uri) : base(uri) { }
        #endregion

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { nco.EmailAddress };
        }

        protected PropertyMapping<string> addressProperty = new PropertyMapping<string>("Address", nco.emailAddress);
        public string Address
        {
            get { return GetValue(addressProperty); }
            set { SetValue(addressProperty, value); }
        }
    }

    class PostalAddress : Resource
    {
        #region Constructors
        public PostalAddress(Uri uri) : base(uri) { }
        #endregion

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { nco.PostalAddress };
        }

        protected PropertyMapping<string> countryProperty = new PropertyMapping<string>("Country", nco.country);
        public string Country
        {
            get { return GetValue(countryProperty); }
            set { SetValue(countryProperty, value); }
        }

        protected PropertyMapping<string> postalCodeProperty = new PropertyMapping<string>("PostalCode", nco.postalcode);
        public string PostalCode
        {
            get { return GetValue(postalCodeProperty); }
            set { SetValue(postalCodeProperty, value); }
        }

         protected PropertyMapping<string> localityProperty = new PropertyMapping<string>("City", nco.locality);
        public string City
        {
            get { return GetValue(localityProperty); }
            set { SetValue(localityProperty, value); }
        }

        protected PropertyMapping<string> streetAddressProperty = new PropertyMapping<string>("StreetAddress", nco.postalcode);
        public string StreetAddress
        {
            get { return GetValue(streetAddressProperty); }
            set { SetValue(streetAddressProperty, value); }
        }
    }

    class Contact : Resource
    {
        #region Constructors
        public Contact(Uri uri) : base(uri) { }
        #endregion

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { nco.Contact };
        }

        protected PropertyMapping<string> fullnameProperty = new PropertyMapping<string>("Fullname", nco.fullname);

        public string Fullname
        {
            get { return GetValue(fullnameProperty); }
            set { SetValue(fullnameProperty, value); }
        }

        protected PropertyMapping<DateTime> birthDateProperty = new PropertyMapping<DateTime>("BirthDate", nco.birthDate);

        public DateTime BirthDate
        {
            get { return GetValue(birthDateProperty); }
            set { SetValue(birthDateProperty, value); }
        }

        protected PropertyMapping<ObservableCollection<EmailAddress>> emailAddressProperty = new PropertyMapping<ObservableCollection<EmailAddress>>("EmailAddresses", nco.hasEmailAddress, new ObservableCollection<EmailAddress>());
        public ObservableCollection<EmailAddress> EmailAddresses
        {
            get { return GetValue(emailAddressProperty); }
            set { SetValue(emailAddressProperty, value); }
        }

        protected PropertyMapping<ObservableCollection<PostalAddress>> postalAddressProperty = new PropertyMapping<ObservableCollection<PostalAddress>>("PostalAddresses", nco.hasPostalAddress, new ObservableCollection<PostalAddress>());
        public ObservableCollection<PostalAddress> PostalAddresses
        {
            get { return GetValue(postalAddressProperty); }
            set { SetValue(postalAddressProperty, value); }
        }

    }

    class PersonContact : Contact
    {
        #region Constructors
        public PersonContact(Uri uri) : base(uri) { }
        #endregion

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { nco.PersonContact };
        }

        protected PropertyMapping<string> nameGivenProperty = new PropertyMapping<string>("NameGiven", nco.nameGiven);
        public string NameGiven
        {
            get { return GetValue(nameGivenProperty); }
            set { SetValue(nameGivenProperty, value); }
        }

        protected PropertyMapping<ObservableCollection<string>> nameAdditonalProperty = new PropertyMapping<ObservableCollection<string>>("NameAdditional", nco.nameAdditional, new ObservableCollection<string>());
        public ObservableCollection<string> NameAdditional
        {
            get { return GetValue(nameAdditonalProperty); }
            set { SetValue(nameAdditonalProperty, value); }
        }

        protected PropertyMapping<string> nameFamilyProperty = new PropertyMapping<string>("NameFamily", nco.nameFamily);
        public string NameFamily
        {
            get { return GetValue(nameFamilyProperty); }
            set { SetValue(nameFamilyProperty, value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; RaisePropertyChanged("IsSelected"); }
        }
    }

    [TestFixture]
    class ResourceBindingTest
    {
        Uri contactListUri = new Uri("semio:test:contactList");
        IStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = Stores.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");

            if (ResourceMappingTest.RegisteredOntology == false)
            {
                OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
                MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
                ResourceMappingTest.RegisteredOntology = true;
            }

        }

        IModel GetModel()
        {

            Uri testModelUri = new Uri("http://localhost:8899/model/TestModel");

            IModel model;
            if (_store.ContainsModel(testModelUri))
            {
                model = _store.GetModel(testModelUri);
            }
            else
            {
                model = _store.CreateModel(testModelUri);
            }

            return model;
        }

        void InitialiseModel(IModel m)
        {
            m.Clear();

            ContactList l = m.CreateResource<ContactList>(contactListUri);

            l.ContainsContact.Add(CreateContact(m, "Hans", new List<string>{"Anton"}, "Meiser", new DateTime(1980, 11, 2), "meiser@test.de", "Deutschland", "Sackgasse 3", "85221", "Dachau"));
            l.ContainsContact.Add(CreateContact(m, "Peter", new List<string>{"Judith", "Ludwig"}, "Meiser", new DateTime(1981, 12, 7), "p.meiser@t-online.de", "Deutschland", "Blubweg 6", "12345", "München"));
            l.ContainsContact.Add(CreateContact(m, "Franz", new List<string> { "Hans", "Wurst" }, "Hubert", new DateTime(1976, 5, 11), "fhubert@t-online.de", "Deutschland", "Siemensstraße 183", "09876", "Berlin"));
            l.ContainsContact.Add(CreateContact(m, "Isabell", new List<string> { "Merlin"}, "Peters", new DateTime(1977, 1, 27), "isab.peters@aol.de", "Deutschland", "Walnussweg 4", "45637", "Bonn"));
            l.ContainsContact.Add(CreateContact(m, "Max", new List<string> (), "Benek", new DateTime(1989, 3, 22), "Max.Benek@aol.de", "Deutschland", "Traunweg 6", "48887", "Schweinfurt"));
            l.ContainsContact.Add(CreateContact(m, "Karsten", new List<string> { "Peter" }, "Oborn", new DateTime(1958, 7, 19), "Superchamp@gmx.de", "Deutschland", "Bei der Wurstfabrik 6", "37439", "Darmstadt"));
            l.ContainsContact.Add(CreateContact(m, "Sabrina", new List<string> { "Hans" }, "Neubert", new DateTime(1960, 8, 15), "Megabirne@gmx.net", "Deutschland", "Hanstraße 1", "55639", "Hanover"));
            l.ContainsContact.Add(CreateContact(m, "Rainer", new List<string> { "Maria" }, "Bader", new DateTime(1970, 4, 26), "Baderainer@web.de", "Deutschland", "Lalaweg 5", "86152", "Augsburg"));
            l.ContainsContact.Add(CreateContact(m, "Maria", new List<string> { "Franz" }, "Roßmann", new DateTime(1968, 10, 6), "Rossmann@web.de", "Deutschland", "Münchner Straße 9", "85123", "Odelzhausen"));
            l.ContainsContact.Add(CreateContact(m, "Helga", new List<string> { "Isabell" }, "Rößler", new DateTime(1988, 2, 1), "Roessler@gmx.de", "Deutschland", "Weiterweg 15", "12345", "München"));
            l.Commit();
        }

        void InitialiseRandomModel(IModel m, int count)
        {
            m.Clear();

            ContactList l = m.CreateResource<ContactList>(contactListUri);

            for (int i = 0; i < count; i++)
            {
                l.ContainsContact.Add(GenerateContact(m));
            }

            l.Commit();
        }

        public class MarkovNameGenerator
        {
            //constructor
            public MarkovNameGenerator(IEnumerable<string> sampleNames, int order, int minLength)
            {
                //fix parameter values
                if (order < 1)
                    order = 1;
                if (minLength < 1)
                    minLength = 1;

                _order = order;
                _minLength = minLength;

                //split comma delimited lines
                foreach (string line in sampleNames)
                {
                    string[] tokens = line.Split(',');
                    foreach (string word in tokens)
                    {
                        string upper = word.Trim().ToUpper();
                        if (upper.Length < order + 1)
                            continue;
                        _samples.Add(upper);
                    }
                }

                //Build chains            
                foreach (string word in _samples)
                {
                    for (int letter = 0; letter < word.Length - order; letter++)
                    {
                        string token = word.Substring(letter, order);
                        List<char> entry = null;
                        if (_chains.ContainsKey(token))
                            entry = _chains[token];
                        else
                        {
                            entry = new List<char>();
                            _chains[token] = entry;
                        }
                        entry.Add(word[letter + order]);
                    }
                }
            }

            //Get the next random name
            public string NextName
            {
                get
                {
                    //get a random token somewhere in middle of sample word                
                    string s = "";
                    do
                    {
                        int n = _rnd.Next(_samples.Count);
                        int nameLength = _samples[n].Length;
                        s = _samples[n].Substring(_rnd.Next(0, _samples[n].Length - _order), _order);
                        while (s.Length < nameLength)
                        {
                            string token = s.Substring(s.Length - _order, _order);
                            char c = GetLetter(token);
                            if (c != '?')
                                s += GetLetter(token);
                            else
                                break;
                        }

                        if (s.Contains(" "))
                        {
                            string[] tokens = s.Split(' ');
                            s = "";
                            for (int t = 0; t < tokens.Length; t++)
                            {
                                if (tokens[t] == "")
                                    continue;
                                if (tokens[t].Length == 1)
                                    tokens[t] = tokens[t].ToUpper();
                                else
                                    tokens[t] = tokens[t].Substring(0, 1) + tokens[t].Substring(1).ToLower();
                                if (s != "")
                                    s += " ";
                                s += tokens[t];
                            }
                        }
                        else
                            s = s.Substring(0, 1) + s.Substring(1).ToLower();
                    }
                    while (_used.Contains(s) || s.Length < _minLength);
                    _used.Add(s);
                    return s;
                }
            }

            //Reset the used names
            public void Reset()
            {
                _used.Clear();
            }

            //private members
            private Dictionary<string, List<char>> _chains = new Dictionary<string, List<char>>();
            private List<string> _samples = new List<string>();
            private List<string> _used = new List<string>();
            private Random _rnd = new Random();
            private int _order;
            private int _minLength;

            //Get a random letter from the chain
            private char GetLetter(string token)
            {
                if (!_chains.ContainsKey(token))
                    return '?';
                List<char> letters = _chains[token];
                int n = _rnd.Next(letters.Count);
                return letters[n];
            }
        }

        Contact GenerateContact(IModel m)
        {
            Random rng = new Random();

            MarkovNameGenerator firstNameGenerator = new MarkovNameGenerator( new List<string>{"Hans", "Peter", "Marie", "Maria", "Tina", "Tim", "Lukas", "Emma", "Tom", "Alina", "Mia", "Emma", "Siegfried", "Judith", "Karl", "Stefan", "Markus", "Martin", "Alfred", "Anton"}, 3, 5);
            string firstName = firstNameGenerator.NextName;

            List<string> additionalNames = new List<string>();
            for (int i = rng.Next(0, 3); i > 0; i--)
            {
                additionalNames.Add(firstNameGenerator.NextName);
            }

            MarkovNameGenerator lastNameGenerator = new MarkovNameGenerator(new List<string> { "Maier", "Meier", "Schmied", "Schmidt", "Schulz", "Roßman", "Müller", "Klein", "Fischer", "Schwarz", "Weber", "Hofman", "Hartman", "Braun", "Koch", "Krüger", "Schröder", "Wolf", "Mayer", "Jung", "Vogel", "Lang", "Fuchs", "Huber" }, 3, 5);
            string lastName = lastNameGenerator.NextName;

            DateTime start = new DateTime(1950, 1, 1);
            int range = ((TimeSpan)(new DateTime(1995, 1, 1) - start)).Days;
            DateTime birthDate = start.AddDays(rng.Next(range));

            string emailAddress = string.Format("{0}.{1}@gmx.de", firstName, lastName);

            return CreateContact(m, firstName, additionalNames, lastName, birthDate, emailAddress, "Deutschland", "Teststraße 27", "123456", "Testhausen"); 
        }

        Contact CreateContact(IModel m, string nameGiven, List<string> nameAdditional, string nameFamily, DateTime birthDate, string emailAddress, string country, string street, string pocode, string city)
        {
            Uri contactUri = new Uri("semio:"+nameGiven+":" + Guid.NewGuid().ToString());
            PersonContact c = m.CreateResource<PersonContact>(contactUri);
            StringBuilder b = new StringBuilder();
            foreach( string n in nameAdditional )
            {
                b.Append(n);
                b.Append(" ");
                c.NameAdditional.Add(n);
            }
            if (b.Length > 1)
            {
                b.Remove(b.Length - 1, 1);
            }
            c.Fullname = string.Format("{0} {1} {2}", nameGiven, b, nameFamily) ;
            c.NameGiven = nameGiven;
            c.NameFamily = nameFamily;
            c.BirthDate = birthDate;
            c.EmailAddresses.Add(CreateEmailAddress(m, emailAddress));
            c.PostalAddresses.Add(CreatePostalAddress(m, country, street, pocode, city));

            c.Commit();
            return c;
        }

        EmailAddress CreateEmailAddress(IModel m, string emailAddress)
        {
            Uri contactUri = new Uri("semio:" + Guid.NewGuid().ToString());
            EmailAddress c = m.CreateResource<EmailAddress>(contactUri);
            c.Address = emailAddress;
            c.Commit();
            return c;
        }

        PostalAddress CreatePostalAddress(IModel m, string country, string street, string pocode, string city)
        {
            Uri contactUri = new Uri("semio:" + Guid.NewGuid().ToString());
            PostalAddress c = m.CreateResource<PostalAddress>(contactUri);
            c.Country = country;
            c.StreetAddress = street;
            c.PostalCode = pocode;
            c.City = city;
            c.Commit();
            return c;
        }
    }
}
