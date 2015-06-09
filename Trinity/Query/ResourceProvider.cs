using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    public class ResourceProvider
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private ResourceQuery _query;

        public ResourceQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public ResourceProvider()
        {
        }

        public ResourceProvider(string name, ResourceQuery query)
        {
        }
    }
}
