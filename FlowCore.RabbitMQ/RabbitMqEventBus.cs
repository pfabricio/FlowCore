using RabbitMQ.Client;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.RabbitMQ.Configuration;

namespace FlowCore.RabbitMQ;

internal sealed class RabbitMqEventBus : IEventBus, IMessageProvider, IDisposable
{
    private readonly RabbitMQOptions _options;
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly IMessageSerializer _serializer;
    private IModel? _publishChannel;
    private bool _started;

    public string Name => "RabbitMQ";

    public RabbitMqEventBus(
        RabbitMQOptions options,
        RabbitMqConnectionManager connectionManager,
        IMessageSerializer serializer)
    {
        _options = options;
        _connectionManager = connectionManager;
        _serializer = serializer;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            _publishChannel = _connectionManager.CreateChannel();
            _started = true;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            _publishChannel?.Close();
            _publishChannel?.Dispose();
            _publishChannel = null;
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

    private Task PublishInternalAsync(Type eventType, object @event, CancellationToken cancellationToken)
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
        var routingKey = EventTypeToRoutingKey(eventType);

        var channel = _publishChannel ??= _connectionManager.CreateChannel();

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            body: body);

        return Task.CompletedTask;
    }

    private static string EventTypeToRoutingKey(Type eventType)
    {
        var name = eventType.Name;
        if (name.EndsWith("Event") && name.Length > 5)
            return name[..^5].ToLowerInvariant();

        return name.ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_started)
        {
            _publishChannel?.Close();
            _publishChannel?.Dispose();
        }
    }
}