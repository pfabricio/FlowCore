namespace FlowCore.Core.Interfaces;

public interface IEventSource
{
    IEnumerable<IEvent> Events { get; }
}
