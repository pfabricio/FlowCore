using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using FlowCore.Core.Interfaces;

namespace FlowCore.Saga;

public sealed class SagaCoordinator
{
    private readonly ISagaStore _store;
    private readonly IServiceScopeFactory _scopeFactory;

    public SagaCoordinator(ISagaStore store, IServiceScopeFactory scopeFactory)
    {
        _store = store;
        _scopeFactory = scopeFactory;
    }

    public async Task<Guid> StartAsync<TSaga>(SagaState initialState, CancellationToken cancellationToken = default)
        where TSaga : Saga
    {
        using var scope = _scopeFactory.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<TSaga>();
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

        using var scope = _scopeFactory.CreateScope();
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

        bool allCompensated = true;
        var stepsByName = new Dictionary<string, SagaStep>(saga.Steps.Count);
        foreach (var s in saga.Steps)
            stepsByName[s.Name] = s;

        for (int i = state.ExecutedSteps.Count - 1; i >= 0; i--)
        {
            var stepName = state.ExecutedSteps[i];

            if (!stepsByName.TryGetValue(stepName, out var step) || step.Compensate is null)
                continue;

            try
            {
                await step.Compensate(new object(), cancellationToken);
            }
            catch
            {
                allCompensated = false;
            }
        }

        state.Status = allCompensated ? SagaStatus.Compensated : SagaStatus.CompensationFailed;
        if (!allCompensated)
            state.FailureReason = "One or more compensation steps failed after original error: " + state.FailureReason;
        await _store.UpdateAsync(state, cancellationToken);
    }

    [RequiresDynamicCode("Extracting SagaId via reflection from event type")]
    [RequiresUnreferencedCode("Extracting SagaId via reflection from event type")]
    private static Guid? ExtractSagaId(IEvent @event)
    {
        var prop = @event.GetType().GetProperty("SagaId")
                   ?? @event.GetType().GetProperty("CorrelationId");

        return prop?.GetValue(@event) is Guid id ? id : null;
    }
}