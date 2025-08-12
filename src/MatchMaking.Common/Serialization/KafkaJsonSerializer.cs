using System.Text.Json;
using Confluent.Kafka;

namespace MatchMaking.Common.Serialization;

public class KafkaJsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return data == null ? null! : JsonSerializer.SerializeToUtf8Bytes(data);
    }
}