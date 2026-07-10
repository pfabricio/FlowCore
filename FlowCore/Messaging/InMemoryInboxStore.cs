using System.Collections.Concurrent;
using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class InMemoryInboxStore : IInboxStore
{
    private readonly ConcurrentDictionary<Guid, InboxMessage> _messages = new();

    public ValueTask<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_messages.ContainsKey(messageId));
    }

    public ValueTask StoreAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.MessageId] = message;
        return ValueTask.CompletedTask;
    }

    public ValueTask CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(olderThan);
        foreach (var kvp in _messages.ToArray())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (kvp.Value.ProcessedAt < cutoff)
                _messages.TryRemove(kvp.Key, out _);
        }
        return ValueTask.CompletedTask;
    }
}