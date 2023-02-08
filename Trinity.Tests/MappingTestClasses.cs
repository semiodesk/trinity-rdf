
using Semiodesk.Trinity.Ontologies;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Semiodesk.Trinity.Test
{
    public abstract class AbstractMappingClass : Resource
    {
        protected PropertyMapping<string> stringTestMapping =
            new PropertyMapping<string>("stringTest", TestOntology.stringTest);

        public string stringTest
        {
            get { return GetValue(stringTestMapping); }
            set { SetValue(stringTestMapping, value); }
        }

        protected AbstractMappingClass(Uri uri) : base(uri) { }
    }

    public class ConcreteMappingClass : AbstractMappingClass
    {
        public override IEnumerable<Class> GetTypes()
        {
            yield return TestOntology.SingleMappingTestClass;
        }

        public ConcreteMappingClass(Uri uri) : base(uri) { }
    }

    public class SingleMappingTestClass : Resource
    {
        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            yield return TestOntology.SingleMappingTestClass;
        }

        protected PropertyMapping<ObservableCollection<string>> stringTestMapping =
            new PropertyMapping<ObservableCollection<string>>("stringTest", TestOntology.stringTest, new ObservableCollection<string>());

        public ObservableCollection<string> stringTest
        {
            get { return GetValue(stringTestMapping); }
            set { SetValue(stringTestMapping, value); }
        }

        #endregion

        #region Constructors

        public SingleMappingTestClass(Uri uri) : base(uri) { }

        #endregion
    }

    public class SingleResourceMappingTestClass : Resource
    {
        #region Constructors

        public SingleResourceMappingTestClass(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.SingleResourceMappingTestClass };
        }

        protected PropertyMapping<ObservableCollection<Resource>> resourceTestMapping =
            new PropertyMapping<ObservableCollection<Resource>>("ResourceTest", TestOntology.resourceTest, new ObservableCollection<Resource>());

        public ObservableCollection<Resource> ResourceTest
        {
            get { return GetValue(resourceTestMapping); }
            set { SetValue(resourceTestMapping, value); }
        }

        #endregion
    }

    public class ResourceMappingTestClass : Resource
    {
        #region Members

        protected PropertyMapping<int> IntegerValueMapping = new PropertyMapping<int>("IntegerValue", TestOntology.intTest);

        public int IntegerValue
        {
            get { return GetValue(IntegerValueMapping); }
            set { SetValue(IntegerValueMapping, value); }
        }

        protected PropertyMapping<ResourceMappingTestClass> ResourceMapping = new PropertyMapping<ResourceMappingTestClass>("Resource", TestOntology.resourceTest);

        public ResourceMappingTestClass Resource
        {
            get { return GetValue(ResourceMapping); }
            set { SetValue(ResourceMapping, value); }
        }

        #endregion

        #region Constructors

        public ResourceMappingTestClass(Uri uri) : base(uri) { }

        #endregion

        #region Methods

        public override IEnumerable<Class> GetTypes()
        {
            yield return TestOntology.ResourceMappingTestClass;
        }

        #endregion
    }

    public class MappingTestClass : Resource
    {
        #region Constructors

        public MappingTestClass(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass };
        }

        protected PropertyMapping<ObservableCollection<int>> intTestMapping =
            new PropertyMapping<ObservableCollection<int>>("intTest", TestOntology.intTest, new ObservableCollection<int>());

        public ObservableCollection<int> intTest
        {
            get { return GetValue(intTestMapping); }
            set { SetValue(intTestMapping, value); }
        }

        protected PropertyMapping<int> uniqueIntTestMapping =
            new PropertyMapping<int>("uniqueIntTest", TestOntology.uniqueIntTest);

        public int uniqueIntTest
        {
            get { return GetValue(uniqueIntTestMapping); }
            set { SetValue(uniqueIntTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<uint>> uintTestMapping =
            new PropertyMapping<ObservableCollection<uint>>("uintTest", TestOntology.uintTest, new ObservableCollection<uint>());

        public ObservableCollection<uint> uintTest
        {
            get { return GetValue(uintTestMapping); }
            set { SetValue(uintTestMapping, value); }
        }


        protected PropertyMapping<uint> uniqueUintTestMapping =
            new PropertyMapping<uint>("uniqueUintTest", TestOntology.uniqueUintTest);

        public uint uniqueUintTest
        {
            get { return GetValue(uniqueUintTestMapping); }
            set { SetValue(uniqueUintTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<string>> stringTestMapping =
            new PropertyMapping<ObservableCollection<string>>("stringTest", TestOntology.stringTest, new ObservableCollection<string>());

        public ObservableCollection<string> stringTest
        {
            get { return GetValue(stringTestMapping); }
            set { SetValue(stringTestMapping, value); }
        }

        protected PropertyMapping<string> uniqueStringTestMapping =
            new PropertyMapping<string>("uniqueStringTest", TestOntology.uniqueStringTest);

        public string uniqueStringTest
        {
            get { return GetValue(uniqueStringTestMapping); }
            set { SetValue(uniqueStringTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<bool>> boolTestMapping =
            new PropertyMapping<ObservableCollection<bool>>("boolTest", TestOntology.boolTest, new ObservableCollection<bool>());

        public ObservableCollection<bool> boolTest
        {
            get { return GetValue(boolTestMapping); }
            set { SetValue(boolTestMapping, value); }
        }

        protected PropertyMapping<bool> uniqueBoolTestMapping =
            new PropertyMapping<bool>("uniqueBoolTest", TestOntology.uniqueBoolTest);

        public bool uniqueBoolTest
        {
            get { return GetValue(uniqueBoolTestMapping); }
            set { SetValue(uniqueBoolTestMapping, value); }
        }

        protected PropertyMapping<float> uniqueFloatTestMapping =
            new PropertyMapping<float>("uniqueFloatTest", TestOntology.uniqueFloatTest);

        public float uniqueFloatTest
        {
            get { return GetValue(uniqueFloatTestMapping); }
            set { SetValue(uniqueFloatTestMapping, value); }
        }

        protected PropertyMapping<double> uniqueDoubleTestMapping =
            new PropertyMapping<double>("uniqueDoubleTest", TestOntology.uniqueDoubleTest);

        public double uniqueDoubleTest
        {
            get { return GetValue(uniqueDoubleTestMapping); }
            set { SetValue(uniqueDoubleTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<double>> doubleTestMapping =
    new PropertyMapping<ObservableCollection<double>>("doubleTest", TestOntology.doubleTest);

        public ObservableCollection<double> DoubleTest
        {
            get { return GetValue(doubleTestMapping); }
            set { SetValue(doubleTestMapping, value); }
        }

        protected PropertyMapping<decimal> uniqueDecimalTestMapping =
            new PropertyMapping<decimal>("uniqueDecimalTest", TestOntology.uniqueDecimalTest);

        public decimal uniqueDecimalTest
        {
            get { return GetValue(uniqueDecimalTestMapping); }
            set { SetValue(uniqueDecimalTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<Resource>> _genericPropertyMapping =
            new PropertyMapping<ObservableCollection<Resource>>("genericProperty", TestOntology.genericTest);

        public ObservableCollection<Resource> genericProperty
        {
            get { return GetValue(_genericPropertyMapping); }
            set { SetValue(_genericPropertyMapping, value); }
        }

        protected PropertyMapping<DateTime> uniqueDateTimeTestMapping =
            new PropertyMapping<DateTime>("uniqueDateTimeTest", TestOntology.uniqueDatetimeTest);

        public DateTime uniqueDateTimeTest
        {
            get { return GetValue(uniqueDateTimeTestMapping); }
            set { SetValue(uniqueDateTimeTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<DateTime>> dateTimeTestMapping =
            new PropertyMapping<ObservableCollection<DateTime>>("dateTimeTest", TestOntology.datetimeTest, new ObservableCollection<DateTime>());

        public ObservableCollection<DateTime> dateTimeTest
        {
            get { return GetValue(dateTimeTestMapping); }
            set { SetValue(dateTimeTestMapping, value); }
        }

        protected PropertyMapping<TimeSpan> uniqueTimeSpanTestMapping =
            new PropertyMapping<TimeSpan>("uniqueTimeSpanTest", TestOntology.uniqueTimespanTest);

        public TimeSpan uniqueTimeSpanTest
        {
            get { return GetValue(uniqueTimeSpanTestMapping); }
            set { SetValue(uniqueTimeSpanTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<TimeSpan>> timeSpanTestMapping =
            new PropertyMapping<ObservableCollection<TimeSpan>>("timeSpanTest", TestOntology.timespanTest, new ObservableCollection<TimeSpan>());

        public ObservableCollection<TimeSpan> timeSpanTest
        {
            get { return GetValue(timeSpanTestMapping); }
            set { SetValue(timeSpanTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<MappingTestClass2>> resourceTestMapping =
            new PropertyMapping<ObservableCollection<MappingTestClass2>>("resourceTest", TestOntology.resourceTest, new ObservableCollection<MappingTestClass2>());

        public ObservableCollection<MappingTestClass2> resourceTest
        {
            get { return GetValue(resourceTestMapping); }
            set { SetValue(resourceTestMapping, value); }
        }

        protected PropertyMapping<MappingTestClass2> uniqueResourceTestMapping =
            new PropertyMapping<MappingTestClass2>("uniqueResourceTest", TestOntology.uniqueResourceTest);

        public MappingTestClass2 uniqueResourceTest
        {
            get { return GetValue(uniqueResourceTestMapping); }
            set { SetValue(uniqueResourceTestMapping, value); }
        }

        protected PropertyMapping<Resource> resPropertyMapping =
            new PropertyMapping<Resource>("resProperty", TestOntology.resTest);

        public Resource resProperty
        {
            get { return (Resource)GetValue(resPropertyMapping); }
            set { SetValue(resPropertyMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<Uri>> uriTestMapping =
            new PropertyMapping<ObservableCollection<Uri>>("uriTest", TestOntology.uriTest, new ObservableCollection<Uri>());

        public ObservableCollection<Uri> uriTest
        {
            get { return GetValue(uriTestMapping); }
            set { SetValue(uriTestMapping, value); }
        }

        protected PropertyMapping<Uri> uniqueUriTestMapping =
            new PropertyMapping<Uri>("uniqueUriTest", TestOntology.uniqueUriTest);

        public Uri uniqueUriTest
        {
            get { return GetValue(uniqueUriTestMapping); }
            set { SetValue(uniqueUriTestMapping, value); }
        }


        #endregion

    }

    public class MappingTestClass2 : Resource
    {
        #region Constructors

        public MappingTestClass2(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass2 };
        }

        protected PropertyMapping<string> uniqueStringTestMapping =
            new PropertyMapping<string>("uniqueStringTest", TestOntology.uniqueStringTest);

        public string uniqueStringTest
        {
            get { return GetValue(uniqueStringTestMapping); }
            set { SetValue(uniqueStringTestMapping, value); }
        }

        #endregion
    }

    public class MappingTestClass3 : MappingTestClass2
    {
        #region Constructors

        public MappingTestClass3(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass3 };
        }

        #endregion
    }

    public class MappingTestClass4 : MappingTestClass3
    {
        #region Constructors

        public MappingTestClass4(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass3 };
        }

        #endregion
    }
    public class MappingTestClass5 : MappingTestClass3
    {
        #region Constructors

        public MappingTestClass5(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass4 };
        }

        #endregion
    }

    public class StringMappingTestClass : Resource
    {
        #region Constructors

        public StringMappingTestClass(Uri uri) : base(uri) { }

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass3 };
        }

        public PropertyMapping<string> randomPropertyTestMapping =
            new PropertyMapping<string>("RandomProperty", "http://www.example.com/property");

        public string RandomProperty
        {
            get { return GetValue(randomPropertyTestMapping); }
            set { SetValue(randomPropertyTestMapping, value); }
        }

        public PropertyMapping<string> uniqueStringTestMapping =
            new PropertyMapping<string>("uniqueStringTest", TestOntology.uniqueStringTest.Uri.OriginalString);

        public string uniqueStringTest
        {
            get { return GetValue(uniqueStringTestMapping); }
            set { SetValue(uniqueStringTestMapping, value); }
        }

        public PropertyMapping<List<string>> stringListTestMapping =
    new PropertyMapping<List<string>>("stringListTest", TestOntology.stringTest);

        public List<string> stringListTest
        {
            get { return GetValue(stringListTestMapping); }
            set { SetValue(stringListTestMapping, value); }
        }

        public PropertyMapping<ObservableCollection<int>> intTestMapping =
            new PropertyMapping<ObservableCollection<int>>("intTest", "semio:test:intTest", new ObservableCollection<int>());

        public ObservableCollection<int> intTest
        {
            get { return GetValue(intTestMapping); }
            set { SetValue(intTestMapping, value); }
        }

        public PropertyMapping<Tuple<string, string>> uniqueLocalizedStringPropertyTestMapping =
new PropertyMapping<Tuple<string, string>>("uniqueLocalizedStringTest", TestOntology.uniqueLocalizedStringTestString);

        public Tuple<string, string> uniqueLocalizedStringTest
        {
            get { return GetValue(uniqueLocalizedStringPropertyTestMapping); }
            set { SetValue(uniqueLocalizedStringPropertyTestMapping, value); }
        }

        public PropertyMapping<List<Tuple<string, string>>> localizedStringPropertyTestMapping =
    new PropertyMapping<List<Tuple<string, string>>>("localizedStringTest", TestOntology.localizedStringTestString);

        public List<Tuple<string, string>> localizedStringTest
        {
            get { return GetValue(localizedStringPropertyTestMapping); }
            set { SetValue(localizedStringPropertyTestMapping, value); }
        }

        #endregion
    }

    public class JsonMappingTestClass : Resource
    {
        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            yield return TestOntology.JsonTestClass;
        }

        protected PropertyMapping<ObservableCollection<string>> stringTestMapping =
            new PropertyMapping<ObservableCollection<string>>("stringTest", TestOntology.stringTest, new ObservableCollection<string>());

        public ObservableCollection<string> stringTest
        {
            get { return GetValue(stringTestMapping); }
            set { SetValue(stringTestMapping, value); }
        }

        #endregion

        #region Constructors

        public JsonMappingTestClass(Uri uri) : base(uri) { }

        #endregion
    }

    public class ContactList : Resource
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

    public class EmailAddress : Resource
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

    public class PostalAddress : Resource
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

    public class Contact : Resource
    {
        #region Constructors
        public Contact(Uri uri) : base(uri) { }
        #endregion

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { nco.Contact };
        }

        protected PropertyMapping<string> fullnameProperty = new PropertyMapping<string>("Fullname", nco.fullname, true);

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
}
