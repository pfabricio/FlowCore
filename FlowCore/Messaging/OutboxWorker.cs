using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public OutboxWorker(IServiceProvider serviceProvider, ILogger<OutboxWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                await foreach (var message in store.GetPendingAsync(stoppingToken))
                {
                    try
                    {
                        var eventType = EventTypeResolver.Resolve(message.EventType);
                        var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();
                        var @event = (IEvent)serializer.Deserialize(eventType, message.Payload);

                        await eventBus.PublishAsync(@event, stoppingToken);
                        await store.MarkAsPublishedAsync(message.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}", message.Id);
                        await store.MarkAsFailedAsync(message.Id, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxWorker error during processing cycle");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }
}