using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using FlowCore.Core.Interfaces;

namespace FlowCore.Scheduling;

internal sealed class InMemoryScheduledMessageStore : IScheduledMessageStore
{
    private readonly ConcurrentDictionary<Guid, ScheduledMessage> _messages = new();

    public ValueTask SaveAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<ScheduledMessage> GetDueMessagesAsync(DateTimeOffset utcNow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var msg in _messages.Values)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (msg.Status == ScheduledMessageStatus.Pending && msg.ExecuteAt <= utcNow)
            {
                msg.Status = ScheduledMessageStatus.Publishing;
                yield return msg;
            }
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
}