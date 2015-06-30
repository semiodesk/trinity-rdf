using Semiodesk.Trinity.Tests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Tests.Cilg
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

        #region Constructors
        public CilgMappingTestClass(Uri uri) : base(uri) { }
        #endregion


        #region

        [RdfProperty(TestOntology.intTestString)]
        public List<int> intTest
        {
            get;
            set;
        }

        [RdfProperty(TestOntology.uniqueIntTestString)]
        public int uniqueIntTest
        {
            get;
            set;
        }

        [RdfProperty(TestOntology.uintTestString)]
        public List<uint> uintTest
        {
            get;
            set;
        }


        
        [RdfProperty(TestOntology.uniqueUintTestString)]
        public uint uniqueUintTest
        {
           get;
            set;
        }



        [RdfProperty(TestOntology.stringTestString)]
        public ObservableCollection<string> stringTest
        {
            get;
            set;
        }


        [RdfProperty(TestOntology.uniqueStringTestString)]
        public string uniqueStringTest
        {
            get;
            set;
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

}
