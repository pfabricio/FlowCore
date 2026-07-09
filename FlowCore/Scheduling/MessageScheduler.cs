using FlowCore.Core;
using FlowCore.Core.Interfaces;

namespace FlowCore.Scheduling;

internal sealed class MessageScheduler : IMessageScheduler
{
    private readonly IScheduledMessageStore _store;
    private readonly IMessageSerializer _serializer;

    public MessageScheduler(IScheduledMessageStore store, IMessageSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    public async ValueTask ScheduleAsync<TEvent>(TEvent @event, DateTimeOffset executeAt, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).Name,
            Payload = _serializer.Serialize(typeof(TEvent), @event),
            ExecuteAt = executeAt,
            Status = ScheduledMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid()
        };

        if (@event is IEvent evt)
        {
            var corrProp = evt.GetType().GetProperty("CorrelationId") ?? evt.GetType().GetProperty("SagaId");
            if (corrProp?.GetValue(evt) is Guid corrId)
                message.CorrelationId = corrId;
        }

        await _store.SaveAsync(message, cancellationToken);
    }

    public async ValueTask ScheduleAfterAsync<TEvent>(TEvent @event, TimeSpan delay, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        await ScheduleAsync(@event, DateTimeOffset.UtcNow.Add(delay), cancellationToken);
    }
}