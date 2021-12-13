using System;

#nullable enable

namespace Dejavu
{
    public interface ISerializeObject
    {
        /// <summary>
        /// Serializing an object instance to a JSON formatted string
        /// </summary>
        string Serialize(object? value);

        /// <summary>
        /// Deserializing an instance from JSON format to given type
        /// </summary>
        object? Deserialize(string value, Type type);
    }
}
