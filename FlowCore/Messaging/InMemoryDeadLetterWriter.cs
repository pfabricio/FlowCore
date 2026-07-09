using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class InMemoryDeadLetterWriter : IDeadLetterWriter
{
    private readonly List<DeadLetterContext> _messages = new();
    private readonly object _lock = new();

    public ValueTask WriteAsync(DeadLetterContext context, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _messages.Add(context);
        }

        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<DeadLetterContext> GetMessages()
    {
        lock (_lock)
        {
            return _messages.ToList();
        }
    }
}