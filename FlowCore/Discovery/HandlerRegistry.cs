using System.Collections.Concurrent;
using FlowCore.Core.Interfaces;

namespace FlowCore.Discovery;

internal sealed class HandlerRegistry : IHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, HandlerDescriptor> _handlers = new();
    private readonly ConcurrentDictionary<Type, HandlerDescriptor[]> _eventHandlers = new();

    public IReadOnlyCollection<HandlerDescriptor> Handlers => _handlers.Values.ToList().AsReadOnly();

    public HandlerDescriptor? GetHandler(Type requestType)
    {
        _handlers.TryGetValue(requestType, out var descriptor);
        return descriptor;
    }

    public IReadOnlyCollection<HandlerDescriptor> GetEventHandlers(Type eventType)
    {
        return _eventHandlers.TryGetValue(eventType, out var handlers)
            ? handlers
            : Array.Empty<HandlerDescriptor>();
    }

    public void Register(HandlerDescriptor descriptor)
    {
        if (descriptor.Kind == HandlerKind.Event)
        {
            _eventHandlers.AddOrUpdate(
                descriptor.RequestType,
                _ => [descriptor],
                (_, existing) => [.. existing, descriptor]);
        }
        else
        {
            _handlers.TryAdd(descriptor.RequestType, descriptor);
        }
    }
}
