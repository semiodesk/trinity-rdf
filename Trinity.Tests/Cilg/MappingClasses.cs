using Semiodesk.Trinity.Test;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Test.Cilg
{
    [RdfClass(TestOntology.SingleMappingTestClassString)]
    public class SingleMappingTestClass : Resource
    {

        #region Constructors
        public SingleMappingTestClass(Uri uri) : base(uri) { }
        #endregion

        #region Mapping


        [RdfProperty(TestOntology.stringTestString)]
        public ObservableCollection<string> stringTest
        {
            get;
            set;
        }

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


        protected PropertyMapping<ObservableCollection<Resource>> resourceTestProperty = new PropertyMapping<ObservableCollection<Resource>>("ResourceTest", TestOntology.resourceTest, new ObservableCollection<Resource>());
        public ObservableCollection<Resource> ResourceTest
        {
            get { return GetValue(resourceTestProperty); }
            set { SetValue(resourceTestProperty, value); }
        }

        #endregion

    }

    [RdfClass(TestOntology.SingleMappingTestClassString)]
    public class CilgMappingTestClass : Resource
    {
        #region Members

        [RdfProperty(TestOntology.intTestString)]
        public List<int> intTest { get; set; }

        [RdfProperty(TestOntology.uniqueIntTestString)]
        public int uniqueIntTest { get; set; }

        [RdfProperty(TestOntology.uintTestString)]
        public List<uint> uintTest { get; set; }

        [RdfProperty(TestOntology.uniqueUintTestString)]
        public uint uniqueUintTest { get; set; }

        [RdfProperty(TestOntology.stringTestString)]
        public ObservableCollection<string> stringTest { get; set; }

        [RdfProperty(TestOntology.uniqueStringTestString)]
        public string uniqueStringTest { get; set; }

        [RdfProperty(TestOntology.resTestString)]
        public Resource uriProperty { get; set; }

        #endregion

        #region Constructors

        public CilgMappingTestClass(Uri uri) : base(uri) 
        {
        }

        #endregion
    }

    [RdfClass(TestOntology.SubMappingTestClassString)]
    public class CilgSubMappingTestClass : CilgMappingTestClass
    {
        #region Constructors

        public CilgSubMappingTestClass(Uri uri) : base(uri) { }

        #endregion
    }

    [RdfClass(TestOntology.SingleMappingTestClassString)]
    [RdfClass(TestOntology.SubMappingTestClassString)]
    public class CilgMultipleMappingTestClass : CilgMappingTestClass
    {
        #region Constructors

        public CilgMultipleMappingTestClass(Uri uri) : base(uri) { }

        #endregion
    }

    [RdfClass(TestOntology.TestClassString)]
    public class CilgListInitializerTestClass3 : Resource
    {
        #region Members

        [RdfProperty(TestOntology.uniqueStringTestString)]
        public string stringTest2 { get; set; }

        [RdfProperty(TestOntology.stringTestString)]
        public List<string> stringTest { get; set; } = new List<string>();

    
        #endregion

        #region Constructors

        public CilgListInitializerTestClass3(Uri uri) : base(uri)
        {
        }

        #endregion
    }

    /*
        [RdfClass(TestOntology.TestClassString)]
        public class CilgListInitializerTestClass : Resource
        {
            #region Members

            [RdfProperty(TestOntology.stringTestString)]
            public List<string> stringTest { get; set; } = new List<string>();


            [RdfProperty(TestOntology.intTestString)]
            public int intTest { get; set; } = 17;

            [RdfProperty(TestOntology.stringTestString)]
            public List<string> stringTest2 { get; set; } = new List<string>() { "bla", "blub" };

            #endregion

            #region Constructors

            public CilgListInitializerTestClass(Uri uri) : base(uri)
            {
            }

            #endregion
        }

        [RdfClass(TestOntology.TestClassString)]
        public class CilgListInitializerTestClass2 : Resource
        {
            #region Members

            protected PropertyMapping<List<string>> stringTestProperty = new PropertyMapping<List<string>>("stringTest", TestOntology.intTestString, new List<string>());
            public List<string> stringTest
            {
                get { return GetValue(stringTestProperty); }
                set { SetValue(stringTestProperty, value); }
            }


            protected PropertyMapping<int> intTestProperty = new PropertyMapping<int>("intTest", TestOntology.intTestString, 17);
            public int intTest
            {
                get { return GetValue(intTestProperty); }
                set { SetValue(intTestProperty, value); }
            } 


            protected PropertyMapping<List<string>> stringTest2Property = new PropertyMapping<List<string>>("stringTest2", TestOntology.intTestString, new List<string>() { "bla", "blub" });
            public List<string> stringTest2
            {
                get { return GetValue(stringTest2Property); }
                set { SetValue(stringTest2Property, value); }
            }
            #endregion

            #region Constructors

            public CilgListInitializerTestClass2(Uri uri) : base(uri)
            {
            }

            #endregion
        }
        */
}
