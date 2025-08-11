using System.Text.Json;
using Confluent.Kafka;

namespace MatchMaking.Common.Serialization;

public class KafkaJsonDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return isNull ? default! : JsonSerializer.Deserialize<T>(data)!;
    }
}