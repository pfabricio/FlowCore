using System.Threading;

namespace FlowCore.Resilience;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public sealed class CircuitBreakerPolicy : IResiliencePolicy
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly object _lock = new();

    private int _failureCount;
    private CircuitState _state = CircuitState.Closed;
    private DateTime _lastFailureTime;
    private DateTime _openTime;

    public CircuitState State
    {
        get { lock (_lock) { return _state; } }
    }

    public CircuitBreakerPolicy(int failureThreshold = 5, TimeSpan? openDuration = null)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        EvaluateState();

        if (_state == CircuitState.Open)
        {
            throw new InvalidOperationException("Circuit breaker is open. Operation rejected.");
        }

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            Reset();
            return result;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            RecordFailure();
            throw;
        }
    }

    private void EvaluateState()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open && DateTime.UtcNow - _openTime >= _openDuration)
            {
                _state = CircuitState.HalfOpen;
            }
        }
    }

    private void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _openTime = DateTime.UtcNow;
            }
        }
    }

    private void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }
}
