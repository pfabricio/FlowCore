using FlowCore.Scheduling;

namespace FlowCore.Core.Interfaces;

public interface IScheduledMessageStore
{
    ValueTask SaveAsync(ScheduledMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ScheduledMessage> GetDueMessagesAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);
    ValueTask MarkAsPublishedAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask MarkAsFailedAsync(Guid id, CancellationToken cancellationToken = default);
}