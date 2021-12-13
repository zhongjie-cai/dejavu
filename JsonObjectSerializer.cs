using System;
using Newtonsoft.Json;

#nullable enable

namespace Dejavu
{
    /// <summary>
    /// Serializes objects to and from JSON strings; can be used as the default ISerializeObject implementation if no special handling is needed from consumer side
    /// </summary>
    public class JsonObjectSerializer : ISerializeObject
    {
        /// <summary>
        /// Serializing an object instance to JSON format
        /// </summary>
        public string Serialize(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            return JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Deserializing an instance from JSON format to given type
        /// </summary>
        public object? Deserialize(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type);
        }
    }
}
