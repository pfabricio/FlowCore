using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;
using FlowCore.Kafka.Configuration;
using FlowCore.Messaging;

namespace FlowCore.Kafka;

internal sealed class KafkaEventBus : IEventBus, IMessageProvider, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<KafkaEventBus> _logger;
    private IProducer<string, byte[]>? _producer;
    private bool _started;

    public string Name => "Kafka";

    public KafkaEventBus(
        KafkaOptions options,
        IMessageSerializer serializer,
        ILogger<KafkaEventBus> logger)
    {
        _options = options;
        _serializer = serializer;
        _logger = logger;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            _producer = new ProducerBuilder<string, byte[]>(
                new ProducerConfig { BootstrapServers = _options.BootstrapServers }).Build();
            _started = true;
            _logger.LogInformation("Kafka EventBus started");
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
            _logger.LogInformation("Kafka EventBus stopped");
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

        _logger.LogDebug("Publishing {EventType} to Kafka topic {Topic}",
            eventType.Name, topic);

        try
        {
            await _producer!.ProduceAsync(topic, new Message<string, byte[]>
            {
                Key = envelope.MessageId.ToString(),
                Value = body
            }, cancellationToken);

            _logger.LogDebug("Published {EventType} successfully", eventType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to Kafka", eventType.Name);
            throw;
        }
    }

    private static string EventTypeToTopic(string eventTypeName)
    {
        if (eventTypeName.EndsWith("Event") && eventTypeName.Length > 5)
            eventTypeName = eventTypeName[..^5];

        return SanitizeTopic(eventTypeName);
    }

    private static string SanitizeTopic(string name)
    {
        return string.Create(name.Length, name, (span, s) =>
        {
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                span[i] = char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.'
                    ? char.ToLowerInvariant(c)
                    : '_';
            }
        });
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