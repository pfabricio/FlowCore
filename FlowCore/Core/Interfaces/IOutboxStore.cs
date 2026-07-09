using FlowCore.Messaging;

namespace FlowCore.Core.Interfaces;

public interface IOutboxStore
{
    ValueTask SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OutboxMessage> GetPendingAsync(CancellationToken cancellationToken = default);
    ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);
    ValueTask MarkAsFailedAsync(Guid messageId, CancellationToken cancellationToken = default);
}