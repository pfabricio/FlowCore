namespace FlowCore.Core.Interfaces;

public interface IHandlerRegistry
{
    IReadOnlyCollection<HandlerDescriptor> Handlers { get; }
    HandlerDescriptor? GetHandler(Type requestType);
    IReadOnlyCollection<HandlerDescriptor> GetEventHandlers(Type eventType);
}
