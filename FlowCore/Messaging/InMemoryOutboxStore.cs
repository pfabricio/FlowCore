using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();
    private readonly object _enumLock = new();

    public ValueTask SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<OutboxMessage> GetPendingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pending = new List<OutboxMessage>();

        foreach (var msg in _messages.Values.ToArray())
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            lock (_enumLock)
            {
                if (msg.Status == OutboxStatus.Pending)
                {
                    msg.Status = OutboxStatus.Processing;
                    pending.Add(msg);
                }
            }
        }

        foreach (var msg in pending)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            yield return msg;
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

    public ValueTask CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(olderThan);
        foreach (var kvp in _messages.ToArray())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var msg = kvp.Value;
            if (msg.Status is OutboxStatus.Published or OutboxStatus.Failed)
            {
                if (msg.PublishedAt.HasValue && msg.PublishedAt.Value < cutoff)
                    _messages.TryRemove(kvp.Key, out _);
            }
        }
        return ValueTask.CompletedTask;
    }
}