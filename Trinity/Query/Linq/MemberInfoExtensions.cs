using System;
using System.Linq;
using System.Reflection;

namespace Semiodesk.Trinity.Query
{
    public static class MemberInfoExtensions
    {
        public static RdfPropertyAttribute TryGetRdfPropertyAttribute(this MemberInfo member)
        {
            Type attributeType = typeof(RdfPropertyAttribute);

            return member.GetCustomAttributes(attributeType, true).FirstOrDefault() as RdfPropertyAttribute;
        }
    }
}
