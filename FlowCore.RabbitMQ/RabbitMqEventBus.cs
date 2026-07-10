using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.RabbitMQ.Configuration;

namespace FlowCore.RabbitMQ;

internal sealed class RabbitMqEventBus : IEventBus, IMessageProvider, IDisposable
{
    private readonly RabbitMQOptions _options;
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private IModel? _publishChannel;
    private bool _started;

    public string Name => "RabbitMQ";

    public RabbitMqEventBus(
        RabbitMQOptions options,
        RabbitMqConnectionManager connectionManager,
        IMessageSerializer serializer,
        ILogger<RabbitMqEventBus> logger)
    {
        _options = options;
        _connectionManager = connectionManager;
        _serializer = serializer;
        _logger = logger;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            _publishChannel = _connectionManager.CreateChannel();
            _started = true;
            _logger.LogInformation("RabbitMQ EventBus started");
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
            _logger.LogInformation("RabbitMQ EventBus stopped");
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

        _logger.LogDebug("Publishing {EventType} to {Exchange}/{RoutingKey}",
            eventType.Name, _options.ExchangeName, routingKey);

        try
        {
            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                body: body);

            _logger.LogDebug("Published {EventType} successfully", eventType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to RabbitMQ", eventType.Name);
            throw;
        }

        return Task.CompletedTask;
    }

    private static string EventTypeToRoutingKey(Type eventType)
    {
        var name = eventType.Name;
        if (name.EndsWith("Event") && name.Length > 5)
            name = name[..^5];

        return SanitizeTopic(name);
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
            _publishChannel?.Close();
            _publishChannel?.Dispose();
        }
    }
}