using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using FlowCore.Core.Interfaces;

namespace FlowCore.Scheduling;

internal sealed class InMemoryScheduledMessageStore : IScheduledMessageStore
{
    private readonly ConcurrentDictionary<Guid, ScheduledMessage> _messages = new();
    private readonly object _enumLock = new();

    public ValueTask SaveAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<ScheduledMessage> GetDueMessagesAsync(DateTimeOffset utcNow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var due = new List<ScheduledMessage>();

        foreach (var msg in _messages.Values.ToArray())
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (msg.ExecuteAt <= utcNow)
            {
                lock (_enumLock)
                {
                    if (msg.Status == ScheduledMessageStatus.Pending)
                    {
                        msg.Status = ScheduledMessageStatus.Publishing;
                        due.Add(msg);
                    }
                }
            }
        }

        foreach (var msg in due)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            yield return msg;
        }

        await ValueTask.CompletedTask;
    }

    public ValueTask MarkAsPublishedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(id, out var msg))
        {
            msg.Status = ScheduledMessageStatus.Published;
            msg.PublishedAt = DateTimeOffset.UtcNow;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask MarkAsFailedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(id, out var msg))
        {
            msg.Status = ScheduledMessageStatus.Failed;
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
            if (msg.Status is ScheduledMessageStatus.Published or ScheduledMessageStatus.Failed)
            {
                if (msg.PublishedAt.HasValue && msg.PublishedAt.Value < cutoff)
                    _messages.TryRemove(kvp.Key, out _);
            }
        }
        return ValueTask.CompletedTask;
    }
}