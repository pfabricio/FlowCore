using FlowCore.Messaging;

namespace FlowCore.Core.Interfaces;

public interface IDeadLetterWriter
{
    ValueTask WriteAsync(DeadLetterContext context, CancellationToken cancellationToken = default);
}