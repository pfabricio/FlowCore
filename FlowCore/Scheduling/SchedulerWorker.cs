using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;

namespace FlowCore.Scheduling;

internal sealed class SchedulerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);

    public SchedulerWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IScheduledMessageStore>();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
                var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();

                var dueMessages = store.GetDueMessagesAsync(DateTimeOffset.UtcNow, stoppingToken);

                await foreach (var message in dueMessages)
                {
                    try
                    {
                        var eventType = EventTypeResolver.Resolve(message.EventType);
                        var eventObj = serializer.Deserialize(eventType, message.Payload);

                        await eventBus.PublishAsync((IEvent)eventObj, stoppingToken);
                        await store.MarkAsPublishedAsync(message.Id, stoppingToken);
                    }
                    catch
                    {
                        await store.MarkAsFailedAsync(message.Id, stoppingToken);
                    }
                }
            }
            catch
            {
                // Log would go here
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }
}