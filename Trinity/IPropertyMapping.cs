using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    internal interface IPropertyMapping
    {
        /// <summary>
        /// The datatype of the the mapped property
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// If the datatype is a collection, this contains the generic type.
        /// </summary>
        Type GenericType { get; }

        /// <summary>
        /// True if the property is mapped to a collection.
        /// </summary>
        bool IsList { get; }

        /// <summary>
        /// The property that should be mapped.
        /// </summary>
        Property Property { get; }

        /// <summary>
        /// The name of the mapped property.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// True if the value has not been set.
        /// </summary>
        bool IsUnsetValue { get; }

        /// <summary>
        /// Language of the value
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// The mapping ignores the language setting and is always non-localized. Only valid if type or generic type is string.
        /// </summary>
        bool LanguageInvariant { get; }

        /// <summary>
        /// Method to test if a type is compatible. In case of collection, the containing type is tested for compatibility.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>True if the type is compatible</returns>
        bool IsTypeCompatible(Type type);

        /// <summary>
        /// Gets the value or values mapped to this property.
        /// </summary>
        /// <returns></returns>
        object GetValueObject();

        /// <summary>
        /// This method is meant to be called from the non-mapped interface. It replaces the current value if 
        /// it is mapped to one value, adds it if the property is mapped to a list.
        /// </summary>
        /// <param name="value"></param>
        void SetOrAddMappedValue(object value);

        /// <summary>
        /// Deletes the containing value and sets the state to unset. In case of a collection, it tries to remove the value from it.
        /// </summary>
        /// <param name="value"></param>
        void RemoveOrResetValue(object value);

        /// <summary>
        /// Clones the mapping of another resource.
        /// </summary>
        /// <param name="other"></param>
        void CloneFrom(IPropertyMapping other);

        /// <summary>
        /// Clears the mapping and resets it.
        /// </summary>
        void Clear();
    }
}
