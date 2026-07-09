using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

public sealed class RetryHandler
{
    private readonly IRetryPolicy _policy;
    private readonly IDeadLetterWriter? _deadLetterWriter;

    public RetryHandler(IRetryPolicy policy, IDeadLetterWriter? deadLetterWriter = null)
    {
        _policy = policy;
        _deadLetterWriter = deadLetterWriter;
    }

    public async Task<bool> ShouldRetryAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        var decision = await _policy.EvaluateAsync(context, cancellationToken);

        if (decision.Action == RetryAction.Retry)
        {
            if (decision.Delay.HasValue && decision.Delay.Value > TimeSpan.Zero)
                await Task.Delay(decision.Delay.Value, cancellationToken);

            return true;
        }

        if (_deadLetterWriter is not null && context.Exception is not null)
        {
            var dlContext = new DeadLetterContext
            {
                Envelope = new MessageEnvelope
                {
                    MessageId = context.MessageId,
                    EventType = context.EventType,
                    Timestamp = context.Timestamp
                },
                Exception = context.Exception,
                RetryCount = context.Attempt,
                FailedAt = DateTimeOffset.UtcNow,
                Metadata = context.Metadata
            };

            await _deadLetterWriter.WriteAsync(dlContext, cancellationToken);
        }

        return false;
    }
}