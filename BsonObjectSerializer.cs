using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

#nullable enable

namespace Dejavu
{
    /// <summary>
    /// Serializes objects to and from BSON strings; can be used as the default ISerializeObject implementation if no special handling is needed from consumer side
    /// </summary>
    public class BsonObjectSerializer : ISerializeObject
    {
        /// <summary>
        /// Serializing an object instance to BSON format
        /// </summary>
        public string Serialize(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (value.GetType().IsPrimitive)
            {
                return JsonConvert.SerializeObject(value);
            }
            var ms = new MemoryStream();
            using (var writer = new BsonDataWriter(ms))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, value);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Deserializing an instance from BSON format to given type
        /// </summary>
        public object? Deserialize(string value, Type type)
        {
            if (type.IsPrimitive)
            {
                return JsonConvert.DeserializeObject(value, type);
            }
            var data = Convert.FromBase64String(value);
            var ms = new MemoryStream(data);
            using (var reader = new BsonDataReader(ms))
            {
                reader.ReadRootValueAsArray = type.IsArray;
                var serializer = new JsonSerializer();
                return serializer.Deserialize(reader, type);
            }
        }
    }
}
