using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FlowCore.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

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
            if (TryResolveFromDi(type, out var invoker))
                return invoker;

            return CreateInvokerWithReflection(type);
        });
    }

    private static bool TryResolveFromDi(Type eventType, [NotNullWhen(true)] out IEventHandlerInvoker? invoker)
    {
        invoker = null;

        try
        {
            var genericInvokerType = typeof(EventHandlerInvoker<>).MakeGenericType(eventType);

            if (Activator.CreateInstance(genericInvokerType) is IEventHandlerInvoker created)
            {
                invoker = created;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    [RequiresDynamicCode("The fallback invoker uses MakeGenericType and Activator.CreateInstance. Use Source Generators for AOT compatibility.")]
    [RequiresUnreferencedCode("The fallback invoker uses MakeGenericType and Activator.CreateInstance. Use Source Generators for AOT compatibility.")]
    private static IEventHandlerInvoker CreateInvokerWithReflection(Type eventType)
    {
        var invokerType = typeof(EventHandlerInvoker<>).MakeGenericType(eventType);
        return (IEventHandlerInvoker)Activator.CreateInstance(invokerType)!;
    }
}
