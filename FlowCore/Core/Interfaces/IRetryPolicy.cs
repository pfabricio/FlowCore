using FlowCore.Messaging;

namespace FlowCore.Core.Interfaces;

public enum RetryAction
{
    Retry,
    Stop
}

public class RetryDecision
{
    public RetryAction Action { get; init; }
    public TimeSpan? Delay { get; init; }
}

public interface IRetryPolicy
{
    ValueTask<RetryDecision> EvaluateAsync(RetryContext context, CancellationToken cancellationToken = default);
}