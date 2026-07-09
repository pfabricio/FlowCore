namespace FlowCore.Saga;

public interface ISagaStore
{
    ValueTask SaveAsync(SagaState state, CancellationToken cancellationToken = default);
    ValueTask<SagaState?> LoadAsync(Guid sagaId, CancellationToken cancellationToken = default);
    ValueTask UpdateAsync(SagaState state, CancellationToken cancellationToken = default);
}