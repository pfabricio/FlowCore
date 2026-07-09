using FlowCore.Core.Interfaces;

namespace FlowCore.Core;

public interface IMessageScheduler
{
    ValueTask ScheduleAsync<TEvent>(TEvent @event, DateTimeOffset executeAt, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    ValueTask ScheduleAfterAsync<TEvent>(TEvent @event, TimeSpan delay, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}