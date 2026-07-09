using Confluent.Kafka;
using FlowCore.Core.Interfaces;
using FlowCore.Kafka.Configuration;
using FlowCore.Messaging;

namespace FlowCore.Kafka;

internal sealed class KafkaConsumerWorker : ConsumerWorker
{
    private readonly KafkaOptions _options;
    private readonly IMessageSerializer _serializer;
    private readonly RetryHandler _retryHandler;

    public KafkaConsumerWorker(
        KafkaOptions options,
        IMessageSerializer serializer,
        IServiceProvider serviceProvider,
        RetryHandler retryHandler)
        : base(serviceProvider, serializer)
    {
        _options = options;
        _serializer = serializer;
        _retryHandler = retryHandler;
    }

    protected override Task ExecuteConsumeAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            EnableAutoCommit = _options.EnableAutoCommit,
            MaxPollIntervalMs = _options.MaxPollIntervalMs,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe($"{_options.TopicPrefix}-*");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var envelope = DeserializeEnvelope(result.Message.Value);

                if (envelope is null || !EnvelopeValidator.IsValid(envelope))
                    continue;

                var attempt = 0;
                Exception? lastException = null;

                do
                {
                    try
                    {
                        attempt++;
                        ProcessEnvelopeAsync(envelope, stoppingToken)
                            .GetAwaiter().GetResult();

                        consumer.Commit();
                        break;
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
                            Provider = "Kafka"
                        };

                        var shouldRetry = _retryHandler.ShouldRetryAsync(context, stoppingToken)
                            .GetAwaiter().GetResult();

                        if (!shouldRetry)
                            break;
                    }
                }
                while (true);

                if (lastException is not null)
                    consumer.Commit();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return Task.CompletedTask;
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