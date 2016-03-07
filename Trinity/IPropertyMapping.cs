using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    internal interface IPropertyMapping
    {
        Type DataType { get; }

        Type GenericType { get; }

        bool IsList { get; }

        Property Property { get; }

        string PropertyName { get; }

        bool IsUnsetValue { get; }

        bool IsTypeCompatible(Type type);

        object GetValueObject();

        void SetOrAddMappedValue(object value);

        void RemoveOrResetValue(object value);

        void CloneFrom(IPropertyMapping other);

        void Clear();
    }
}
