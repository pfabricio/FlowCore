using System.Text.Json;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class SystemTextJsonSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, Options);
    }

    public byte[] Serialize(Type type, object value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, type, Options);
    }

    public T Deserialize<T>(byte[] data)
    {
        return JsonSerializer.Deserialize<T>(data, Options)
            ?? throw new InvalidOperationException("Deserialization returned null.");
    }

    public object Deserialize(Type type, byte[] data)
    {
        return JsonSerializer.Deserialize(data, type, Options)
            ?? throw new InvalidOperationException($"Deserialization returned null for type '{type.FullName}'.");
    }
}