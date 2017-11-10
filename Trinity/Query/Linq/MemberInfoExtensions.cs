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

        public static TAttribute TryGetCustomAttribute<TAttribute>(this MemberInfo member) where TAttribute : Attribute
        {
            return member.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;
        }
    }
}
