using Microsoft.Extensions.DependencyInjection;
using FlowCore.Core.Interfaces;

namespace FlowCore.Saga;

public sealed class SagaCoordinator
{
    private readonly ISagaStore _store;
    private readonly IServiceProvider _serviceProvider;

    public SagaCoordinator(ISagaStore store, IServiceProvider serviceProvider)
    {
        _store = store;
        _serviceProvider = serviceProvider;
    }

    public async Task<Guid> StartAsync<TSaga>(SagaState initialState, CancellationToken cancellationToken = default)
        where TSaga : Saga
    {
        var saga = CreateSaga<TSaga>();
        await saga.DefineStepsAsync();

        initialState.SagaType = saga.Name;
        initialState.Status = SagaStatus.Running;
        initialState.StartedAt = DateTimeOffset.UtcNow;

        if (initialState.SagaId == Guid.Empty)
            initialState.SagaId = Guid.NewGuid();

        await _store.SaveAsync(initialState, cancellationToken);

        return initialState.SagaId;
    }

    public async Task HandleEventAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();

        using var scope = _serviceProvider.CreateScope();
        var sagas = scope.ServiceProvider.GetServices<Saga>();

        foreach (var saga in sagas)
        {
            await saga.DefineStepsAsync();

            for (int i = 0; i < saga.Steps.Count; i++)
            {
                var step = saga.Steps[i];

                if (step.EventType != eventType)
                    continue;

                var sagaId = ExtractSagaId(@event);
                if (sagaId is null)
                    continue;

                var state = await _store.LoadAsync(sagaId.Value, cancellationToken);
                if (state is null || state.Status != SagaStatus.Running)
                    continue;

                if (state.CurrentStep != i)
                    continue;

                try
                {
                    await step.Execute(@event, cancellationToken);
                    state.CurrentStep = i + 1;
                    state.ExecutedSteps.Add(step.Name);

                    if (state.CurrentStep >= saga.Steps.Count)
                    {
                        state.Status = SagaStatus.Completed;
                        state.CompletedAt = DateTimeOffset.UtcNow;
                    }

                    await _store.UpdateAsync(state, cancellationToken);
                }
                catch (Exception ex)
                {
                    state.Status = SagaStatus.Failed;
                    state.FailureReason = ex.Message;
                    await _store.UpdateAsync(state, cancellationToken);

                    await CompensateAsync(saga, state, cancellationToken);
                }

                break;
            }
        }
    }

    private async Task CompensateAsync(Saga saga, SagaState state, CancellationToken cancellationToken)
    {
        state.Status = SagaStatus.Compensating;
        await _store.UpdateAsync(state, cancellationToken);

        for (int i = state.ExecutedSteps.Count - 1; i >= 0; i--)
        {
            var stepName = state.ExecutedSteps[i];
            var step = saga.Steps.FirstOrDefault(s => s.Name == stepName);

            if (step?.Compensate is null)
                continue;

            try
            {
                // We need the original event - stored in state data
                await step.Compensate(new object(), cancellationToken);
            }
            catch
            {
                // Log compensation failure, continue with next
            }
        }

        state.Status = SagaStatus.Compensated;
        await _store.UpdateAsync(state, cancellationToken);
    }

    private static TSaga CreateSaga<TSaga>() where TSaga : Saga
    {
        return Activator.CreateInstance<TSaga>();
    }

    private static Guid? ExtractSagaId(IEvent @event)
    {
        var prop = @event.GetType().GetProperty("SagaId")
                   ?? @event.GetType().GetProperty("CorrelationId");

        return prop?.GetValue(@event) is Guid id ? id : null;
    }
}