using System;

namespace Semiodesk.Trinity.Query
{
    class TypeHelper
    {
        public static object GetDefaultValue(Type type)
        {
            if(type == typeof(String))
            {
                return "";
            }
            else
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
        }
    }
}
