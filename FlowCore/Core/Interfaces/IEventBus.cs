namespace FlowCore.Core.Interfaces;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
