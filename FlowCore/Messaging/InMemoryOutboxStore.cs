using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    public ValueTask SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<OutboxMessage> GetPendingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var msg in _messages.Values)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (msg.Status == OutboxStatus.Pending)
            {
                msg.Status = OutboxStatus.Processing;
                yield return msg;
            }
        }

        await ValueTask.CompletedTask;
    }

    public ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var msg))
        {
            msg.Status = OutboxStatus.Published;
            msg.PublishedAt = DateTimeOffset.UtcNow;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask MarkAsFailedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var msg))
        {
            msg.Status = OutboxStatus.Failed;
            msg.RetryCount++;
        }

        return ValueTask.CompletedTask;
    }
}