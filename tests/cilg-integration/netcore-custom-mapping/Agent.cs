using System;
using System.Collections.Generic;
using Semiodesk.Trinity;

namespace NetCore_Test
{
    [RdfClass(FOAF.Agent)]
    public class Agent : Resource
    {
        public bool Changed = false;

        public Agent(Uri uri)
            : base(uri)
        {
        }

        public override IEnumerable<Class> GetTypes()
        {
            Changed = true;
            return new Class[] { foaf.Agent };
        }

        protected readonly PropertyMapping<string> nameMapping = new PropertyMapping<string>("Name", FOAF.name);
        [RdfProperty(FOAF.name)]
        public string Name
        {
            get => GetValue(nameMapping);
            set => SetValue(nameMapping, value);
        }

        protected readonly PropertyMapping<List<Resource>> mboxMapping = new PropertyMapping<List<Resource>>("EMailAccounts", FOAF.mbox);
        [RdfProperty(FOAF.mbox)]
        public List<Resource> EMailAccounts
        {
            get => GetValue(mboxMapping);
            set => SetValue(mboxMapping, value);
        }

    }

}
