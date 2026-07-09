using Microsoft.Extensions.DependencyInjection;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class InMemoryEventBus : IEventBus
{
    private readonly DispatcherCache _cache;
    private readonly IServiceProvider _serviceProvider;

    public InMemoryEventBus(DispatcherCache cache, IServiceProvider serviceProvider)
    {
        _cache = cache;
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }

    public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var invoker = _cache.GetOrCreate(@event.GetType());
        return invoker.InvokeAllAsync(_serviceProvider, @event, cancellationToken);
    }
}
