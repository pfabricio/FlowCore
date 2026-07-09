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
}