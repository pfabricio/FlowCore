using System.Collections.Immutable;
using FlowCore.Core.Interfaces;

namespace FlowCore.Testing;

public sealed class FakeEventBus : IEventBus
{
    private readonly List<object> _published = [];

    public IReadOnlyCollection<object> Published => _published.ToImmutableList();

    public IReadOnlyCollection<TEvent> PublishedOfType<TEvent>() where TEvent : IEvent
    {
        return _published.OfType<TEvent>().ToImmutableArray();
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        _published.Add(@event);
        return Task.CompletedTask;
    }

    public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        _published.Add(@event);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _published.Clear();
    }
}
