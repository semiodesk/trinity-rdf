using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

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

        public static Type GetMemberType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return (member as EventInfo).EventHandlerType;
                case MemberTypes.Field:
                    return (member as FieldInfo).FieldType;
                case MemberTypes.Method:
                    return (member as MethodInfo).ReturnType;
                case MemberTypes.Property:
                    return (member as PropertyInfo).PropertyType;
                default:
                    throw new ArgumentException
                    (
                     "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                    );
            }
        }

        public static bool Is(this MemberInfo member, Type type)
        {
            return type.IsAssignableFrom(member.GetMemberType());
        }

        public static bool IsSystemType(this MemberInfo member)
        {
            HashSet<Type> systemTypes = new HashSet<Type>()
            {
                typeof(DateTime),
                typeof(String)
            };

            return systemTypes.Contains(member.DeclaringType);
        }
    }
}
