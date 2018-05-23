using System;

namespace Semiodesk.Trinity.Query
{
    class TypeHelper
    {
        public static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
