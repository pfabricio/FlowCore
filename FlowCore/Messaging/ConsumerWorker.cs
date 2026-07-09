using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;

namespace FlowCore.Messaging;

public abstract class ConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageSerializer _serializer;

    protected ConsumerWorker(IServiceProvider serviceProvider, IMessageSerializer serializer)
    {
        _serviceProvider = serviceProvider;
        _serializer = serializer;
    }

    protected abstract Task ExecuteConsumeAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteConsumeAsync(stoppingToken);
    }

    protected async Task ProcessEnvelopeAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(envelope.EventType) || envelope.Payload.Length == 0)
            return;

        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<IInboxStore>();
        var activityFactory = scope.ServiceProvider.GetRequiredService<IActivityFactory>();

        if (store is not null && await store.ExistsAsync(envelope.MessageId, cancellationToken))
            return;

        var activity = activityFactory.Start(
            $"consume.{envelope.EventType}",
            ActivityKindType.Consumer,
            envelope.CorrelationId.ToString());

        try
        {
            activity?.SetTag("event.type", envelope.EventType);
            activity?.SetTag("message.id", envelope.MessageId.ToString());
            activity?.SetTag("correlation.id", envelope.CorrelationId.ToString());

            using var innerScope = _serviceProvider.CreateScope();
            var eventBus = innerScope.ServiceProvider.GetRequiredService<IEventBus>();
            var serializer = innerScope.ServiceProvider.GetRequiredService<IMessageSerializer>();

            var eventType = EventTypeResolver.Resolve(envelope.EventType);
            var eventObj = serializer.Deserialize(eventType, envelope.Payload);

            await eventBus.PublishAsync((IEvent)eventObj, cancellationToken);

            if (store is not null)
            {
                await store.StoreAsync(new InboxMessage
                {
                    MessageId = envelope.MessageId,
                    EventType = envelope.EventType,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Consumer = GetType().Name,
                    CorrelationId = envelope.CorrelationId
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            activity?.SetException(ex);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }
}