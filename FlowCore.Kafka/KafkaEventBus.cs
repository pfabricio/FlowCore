using Confluent.Kafka;
using FlowCore.Core.Interfaces;
using FlowCore.Kafka.Configuration;
using FlowCore.Messaging;

namespace FlowCore.Kafka;

internal sealed class KafkaEventBus : IEventBus, IMessageProvider, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly IMessageSerializer _serializer;
    private IProducer<string, byte[]>? _producer;
    private bool _started;

    public string Name => "Kafka";

    public KafkaEventBus(
        KafkaOptions options,
        IMessageSerializer serializer)
    {
        _options = options;
        _serializer = serializer;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            _producer = new ProducerBuilder<string, byte[]>(
                new ProducerConfig { BootstrapServers = _options.BootstrapServers }).Build();
            _started = true;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            _producer?.Flush();
            _producer?.Dispose();
            _producer = null;
            _started = false;
        }
        return ValueTask.CompletedTask;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        return PublishInternalAsync(typeof(TEvent), @event, cancellationToken);
    }

    public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        return PublishInternalAsync(@event.GetType(), @event, cancellationToken);
    }

    private async Task PublishInternalAsync(Type eventType, object @event, CancellationToken cancellationToken)
    {
        var envelope = new MessageEnvelope
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            EventType = eventType.Name,
            Timestamp = DateTimeOffset.UtcNow,
            Payload = _serializer.Serialize(eventType, @event)
        };

        var body = _serializer.Serialize(envelope);
        var topic = EventTypeToTopic(eventType.Name);

        if (!_started)
            await StartAsync(cancellationToken);

        await _producer!.ProduceAsync(topic, new Message<string, byte[]>
        {
            Key = envelope.MessageId.ToString(),
            Value = body
        }, cancellationToken);
    }

    private static string EventTypeToTopic(string eventTypeName)
    {
        if (eventTypeName.EndsWith("Event") && eventTypeName.Length > 5)
            return eventTypeName[..^5].ToLowerInvariant();

        return eventTypeName.ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_started)
        {
            _producer?.Flush();
            _producer?.Dispose();
        }
    }
}