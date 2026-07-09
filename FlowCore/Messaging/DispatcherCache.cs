using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal interface IEventHandlerInvoker
{
    Task InvokeAllAsync(IServiceProvider serviceProvider, IEvent @event, CancellationToken cancellationToken);
}

internal sealed class EventHandlerInvoker<TEvent> : IEventHandlerInvoker
    where TEvent : IEvent
{
    public async Task InvokeAllAsync(IServiceProvider serviceProvider, IEvent @event, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync((TEvent)@event, cancellationToken);
        }
    }
}

internal sealed class DispatcherCache
{
    private readonly ConcurrentDictionary<Type, IEventHandlerInvoker> _cache = new();

    public IEventHandlerInvoker GetOrCreate(Type eventType)
    {
        return _cache.GetOrAdd(eventType, static type =>
        {
            var invokerType = typeof(EventHandlerInvoker<>).MakeGenericType(type);
            return (IEventHandlerInvoker)Activator.CreateInstance(invokerType)!;
        });
    }
}
