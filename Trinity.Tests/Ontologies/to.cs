using System;

namespace Semiodesk.Trinity.Tests
{
    public class to
    {
        public static readonly Uri Namespace = new Uri("semio:test");
        
        public Uri GetNamespace() { return Namespace; }
        
        public static readonly string Prefix = "test";
        
        public string GetPrefix() { return Prefix; }

        public static readonly Class SingleMappingTestClass = new Class(new Uri(SingleMappingTestClassString));
        
        public const string SingleMappingTestClassString = "semio:test:SingleMappingTestClass";
        
        public static readonly Class SingleResourceMappingTestClass = new Class(new Uri("semio:test:SingleResourceMappingTestClass"));
        
        public static readonly Class ResourceMappingTestClass = new Class(new Uri("semio:test:ResourceMappingTestClass"));

        public const string SubMappingTestClassString = "semio:test:SubMappingTestClass";
        
        public static readonly Class SubMappingTestClass = new Class(new Uri(SubMappingTestClassString));

        public const string TestClassString = "semio:test:TestClass";
        
        public static readonly Class TestClass = new Class(new Uri(TestClassString));
        
        public static readonly Class TestClass2 = new Class(new Uri("semio:test:TestClass2"));
        
        public static readonly Class TestClass3 = new Class(new Uri("semio:test:TestClass3"));
        
        public static readonly Class TestClass4 = new Class(new Uri("semio:test:TestClass4"));

        public const string genericTestString = "semio:test:genericTest";
        
        public static readonly Property genericTest = new Property(new Uri(genericTestString));

        public const string intTestString = "semio:test:intTest";
        
        public static readonly Property intTest = new Property(new Uri(intTestString));
        
        public const string uniqueIntTestString = "semio:test:uniqueIntTest";
        
        public static readonly Property uniqueIntTest = new Property(new Uri(uniqueIntTestString));

        public const string uintTestString ="semio:test:uintTest";
        
        public static readonly Property uintTest = new Property(new Uri(uintTestString));
        
        public const string uniqueUintTestString = "semio:test:uniqueUintTest";
        
        public static readonly Property uniqueUintTest = new Property(new Uri(uniqueUintTestString));

        public const string stringTestString = "semio:test:stringTest";
        
        public static readonly Property stringTest = new Property(new Uri(stringTestString));

        public const  string uniqueStringTestString = "semio:test:uniqueStringTest";
        
        public static readonly Property uniqueStringTest = new Property(new Uri(uniqueStringTestString));

        public const string localizedStringTestString = "semio:test:localizedStringTest";
        
        public static readonly Property localizedStringTest = new Property(new Uri(localizedStringTestString));

        public const string uniqueLocalizedStringTestString = "semio:test:uniqueLocalizedStringTest";
        
        public static readonly Property uniqueLocalizedStringTest = new Property(new Uri(uniqueLocalizedStringTestString));

        public const string localizedStringCultureTestString = "semio:test:localizedStringCultureTest";
        
        public static readonly Property localizedStringCultureTest = new Property(new Uri(localizedStringCultureTestString));

        public const string uniqueLocalizedStringCultureTestString = "semio:test:uniqueLocalizedStringCultureTest";
        
        public static readonly Property uniqueLocalizedStringCultureTest = new Property(new Uri(uniqueLocalizedStringCultureTestString));

        public static readonly Property floatTest = new Property(new Uri("semio:test:floatTest"));
        
        public static readonly Property uniqueFloatTest = new Property(new Uri("semio:test:uniqueFloatTest"));

        public static readonly Property doubleTest = new Property(new Uri("semio:test:doubleTest"));
        
        public static readonly Property uniqueDoubleTest = new Property(new Uri("semio:test:uniqueDoubleTest"));

        public static readonly Property decimalTest = new Property(new Uri("semio:test:decimalTest"));
        
        public static readonly Property uniqueDecimalTest = new Property(new Uri("semio:test:uniqueDecimalTest"));

        public static readonly Property boolTest = new Property(new Uri("semio:test:boolTest"));
        
        public static readonly Property uniqueBoolTest = new Property(new Uri("semio:test:uniqueBoolTest"));

        public static readonly Property datetimeTest = new Property(new Uri("semio:test:datetimeTest"));
        
        public static readonly Property uniqueDatetimeTest = new Property(new Uri("semio:test:uniqueDatetimeTest"));

        public static readonly Property timespanTest = new Property(new Uri("semio:test:timespanTest"));
        
        public static readonly Property uniqueTimespanTest = new Property(new Uri("semio:test:uniqueTimespanTest"));

        public static readonly Property resourceTest = new Property(new Uri("semio:test:resourceTest"));
        
        public static readonly Property uniqueResourceTest = new Property(new Uri("semio:test:uniqueResourceTest"));

        public const string resTestString = "semio:test:resTest";
        
        public static readonly Property resTest = new Property(new Uri(resTestString));

        public static readonly Property uriTest = new Property(new Uri("semio:test:uriTest"));
        
        public static readonly Property uniqueUriTest = new Property(new Uri("semio:test:uniqueUriTest"));

        public const string JsonTestClassUri = "http://localhost/JsonTestClass";
        
        public static readonly Class JsonTestClass = new Class(new Uri(JsonTestClassUri));
    }
}