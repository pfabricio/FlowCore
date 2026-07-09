using FlowCore.Messaging;

namespace FlowCore.Core.Interfaces;

public interface IInboxStore
{
    ValueTask<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);
    ValueTask StoreAsync(InboxMessage message, CancellationToken cancellationToken = default);
}