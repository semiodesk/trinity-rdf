using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Semiodesk.Trinity.Tests.Cilg
{
    [RdfClass(TestOntology.SingleMappingTestClassString)]
    public partial class SingleMappingTestClass : Resource
    {

        #region Constructors
        public SingleMappingTestClass(Uri uri) : base(uri) { }
        #endregion

        #region Mapping


        [RdfProperty(TestOntology.stringTestString)]
        public partial ObservableCollection<string> stringTest
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
    public partial class CilgMappingTestClass : Resource
    {
        #region Members

        [RdfProperty(TestOntology.intTestString)]
        public partial List<int> intTest { get; set; }

        [RdfProperty(TestOntology.uniqueIntTestString)]
        public partial int uniqueIntTest { get; set; }

        [RdfProperty(TestOntology.uintTestString)]
        public partial List<uint> uintTest { get; set; }

        [RdfProperty(TestOntology.uniqueUintTestString)]
        public partial uint uniqueUintTest { get; set; }

        [RdfProperty(TestOntology.stringTestString)]
        public partial ObservableCollection<string> stringTest { get; set; }

        [RdfProperty(TestOntology.uniqueStringTestString)]
        public partial string uniqueStringTest { get; set; }

        [RdfProperty(TestOntology.resTestString)]
        public partial Resource uriProperty { get; set; }

        #endregion

        #region Constructors

        public CilgMappingTestClass(Uri uri) : base(uri)
        {
        }

        #endregion
    }

    [RdfClass(TestOntology.SubMappingTestClassString)]
    public partial class CilgSubMappingTestClass : CilgMappingTestClass
    {
        #region Constructors

        public CilgSubMappingTestClass(Uri uri) : base(uri) { }

        #endregion
    }

    [RdfClass(TestOntology.SingleMappingTestClassString)]
    [RdfClass(TestOntology.SubMappingTestClassString)]
    public partial class CilgMultipleMappingTestClass : CilgMappingTestClass
    {
        #region Constructors

        public CilgMultipleMappingTestClass(Uri uri) : base(uri) { }

        #endregion
    }

    [RdfClass(TestOntology.TestClassString)]
    public partial class CilgListInitializerTestClass3 : Resource
    {
        #region Members

        [RdfProperty(TestOntology.uniqueStringTestString)]
        public partial string stringTest2 { get; set; }

        // NOTE: partial properties cannot carry a field initializer; the generator seeds the
        // PropertyMapping with an empty List<string> default instead.
        [RdfProperty(TestOntology.stringTestString)]
        public partial List<string> stringTest { get; set; }

        #endregion

        #region Constructors

        public CilgListInitializerTestClass3(Uri uri) : base(uri)
        {
        }

        #endregion
    }
}
