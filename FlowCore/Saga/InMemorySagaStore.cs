using System.Collections.Concurrent;

namespace FlowCore.Saga;

internal sealed class InMemorySagaStore : ISagaStore
{
    private readonly ConcurrentDictionary<Guid, SagaState> _sagas = new();

    public ValueTask SaveAsync(SagaState state, CancellationToken cancellationToken = default)
    {
        _sagas[state.SagaId] = state;
        return ValueTask.CompletedTask;
    }

    public ValueTask<SagaState?> LoadAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _sagas.TryGetValue(sagaId, out var state);
        return ValueTask.FromResult(state);
    }

    public ValueTask UpdateAsync(SagaState state, CancellationToken cancellationToken = default)
    {
        _sagas[state.SagaId] = state;
        return ValueTask.CompletedTask;
    }

    public ValueTask CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(olderThan);
        foreach (var kvp in _sagas.ToArray())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var state = kvp.Value;
            if (state.Status is SagaStatus.Completed or SagaStatus.Compensated
                or SagaStatus.Failed or SagaStatus.CompensationFailed)
            {
                if (state.CompletedAt.HasValue && state.CompletedAt.Value < cutoff)
                    _sagas.TryRemove(kvp.Key, out _);
            }
        }
        return ValueTask.CompletedTask;
    }
}