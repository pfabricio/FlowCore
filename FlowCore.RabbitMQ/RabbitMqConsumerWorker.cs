using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.RabbitMQ.Configuration;

namespace FlowCore.RabbitMQ;

internal sealed class RabbitMqConsumerWorker : ConsumerWorker
{
    private readonly RabbitMQOptions _options;
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly IMessageSerializer _serializer;
    private readonly RetryHandler _retryHandler;

    public RabbitMqConsumerWorker(
        RabbitMQOptions options,
        RabbitMqConnectionManager connectionManager,
        IMessageSerializer serializer,
        IServiceProvider serviceProvider,
        RetryHandler retryHandler)
        : base(serviceProvider, serializer)
    {
        _options = options;
        _connectionManager = connectionManager;
        _serializer = serializer;
        _retryHandler = retryHandler;
    }

    private IModel? _channel;

    protected override async Task ExecuteConsumeAsync(CancellationToken stoppingToken)
    {
        _channel = _connectionManager.CreateChannel();
        try
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                var envelope = DeserializeEnvelope(ea.Body.ToArray());

                if (envelope is null || !EnvelopeValidator.IsValid(envelope))
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                var attempt = 0;
                Exception? lastException = null;

                do
                {
                    try
                    {
                        attempt++;
                        await ProcessEnvelopeAsync(envelope, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;

                        var context = new RetryContext
                        {
                            Attempt = attempt,
                            Exception = ex,
                            EventType = envelope.EventType,
                            MessageId = envelope.MessageId,
                            Timestamp = DateTimeOffset.UtcNow,
                            Provider = "RabbitMQ"
                        };

                        var shouldRetry = await _retryHandler.ShouldRetryAsync(context, stoppingToken);
                        if (!shouldRetry)
                            break;
                    }
                }
                while (true);

                _channel.BasicNack(ea.DeliveryTag, false, false);
            };

            var queueName = _channel.QueueDeclare(
                queue: $"{_options.QueuePrefix}-{Guid.NewGuid()}",
                durable: false,
                exclusive: true,
                autoDelete: true);

            _channel.QueueBind(queueName, _options.ExchangeName, "*");
            _channel.BasicConsume(queueName, false, consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            if (_channel is { IsOpen: true })
            {
                _channel.Close();
                _channel.Dispose();
                _channel = null;
            }
        }
    }

    private MessageEnvelope? DeserializeEnvelope(byte[] body)
    {
        try
        {
            return _serializer.Deserialize<MessageEnvelope>(body);
        }
        catch
        {
            return null;
        }
    }
}