using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Saga;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowCore.Tests;

public class SagaTests
{
    [Fact]
    public async Task StartAsync_ShouldCreateSagaWithRunningStatus()
    {
        var store = new InMemorySagaStore();
        var services = new ServiceCollection();
        services.AddSingleton<ISagaStore>(store);
        services.AddTransient<global::FlowCore.Saga.Saga, TestOrderSaga>();
        var provider = services.BuildServiceProvider();
        var coordinator = new SagaCoordinator(store, provider);

        var state = new SagaState { CorrelationId = Guid.NewGuid() };
        var sagaId = await coordinator.StartAsync<TestOrderSaga>(state);

        sagaId.Should().NotBeEmpty();
        var loaded = await store.LoadAsync(sagaId);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SagaStatus.Running);
    }

    [Fact]
    public async Task StartAsync_WithExistingSagaId_ShouldKeepIt()
    {
        var store = new InMemorySagaStore();
        var services = new ServiceCollection();
        services.AddSingleton<ISagaStore>(store);
        services.AddTransient<global::FlowCore.Saga.Saga, TestOrderSaga>();
        var provider = services.BuildServiceProvider();
        var coordinator = new SagaCoordinator(store, provider);

        var expectedId = Guid.NewGuid();
        var state = new SagaState
        {
            SagaId = expectedId,
            CorrelationId = Guid.NewGuid()
        };
        var sagaId = await coordinator.StartAsync<TestOrderSaga>(state);

        sagaId.Should().Be(expectedId);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldExecuteMatchingStep()
    {
        var store = new InMemorySagaStore();
        var services = new ServiceCollection();
        services.AddSingleton<ISagaStore>(store);
        services.AddTransient<global::FlowCore.Saga.Saga, TestOrderSaga>();
        var provider = services.BuildServiceProvider();
        var coordinator = new SagaCoordinator(store, provider);

        var correlationId = Guid.NewGuid();
        var state = new SagaState
        {
            SagaId = correlationId,
            CorrelationId = correlationId,
            Status = SagaStatus.Running,
            CurrentStep = 0
        };
        await store.SaveAsync(state);

        await coordinator.HandleEventAsync(new OrderCreatedEvent(correlationId));

        var loaded = await store.LoadAsync(correlationId);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SagaStatus.Completed);
        loaded.ExecutedSteps.Should().Contain("CreateOrder");
    }

    [Fact]
    public async Task HandleEventAsync_WhenStepFails_ShouldCompensate()
    {
        var store = new InMemorySagaStore();
        var saga = new TestFailingSaga();

        var services = new ServiceCollection();
        services.AddSingleton<ISagaStore>(store);
        services.AddTransient<global::FlowCore.Saga.Saga>(_ => saga);
        var provider = services.BuildServiceProvider();
        var coordinator = new SagaCoordinator(store, provider);

        var correlationId = Guid.NewGuid();
        var state = new SagaState
        {
            SagaId = correlationId,
            CorrelationId = correlationId,
            Status = SagaStatus.Running,
            CurrentStep = 0
        };
        await store.SaveAsync(state);

        await coordinator.HandleEventAsync(new OrderCreatedEvent(correlationId));

        var loaded = await store.LoadAsync(correlationId);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SagaStatus.Compensated);
        loaded.FailureReason.Should().Be("Step failed");
    }

    [Fact]
    public async Task HandleEventAsync_WithNonMatchingStep_ShouldSkip()
    {
        var store = new InMemorySagaStore();
        var saga = new TestOrderSaga();
        await saga.DefineStepsAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ISagaStore>(store);
        services.AddTransient<global::FlowCore.Saga.Saga>(_ => saga);
        var provider = services.BuildServiceProvider();
        var coordinator = new SagaCoordinator(store, provider);

        var correlationId = Guid.NewGuid();
        var state = new SagaState
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = correlationId,
            Status = SagaStatus.Running,
            CurrentStep = 0
        };
        await store.SaveAsync(state);

        await coordinator.HandleEventAsync(new OtherTestEvent());

        var loaded = await store.LoadAsync(state.SagaId);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SagaStatus.Running);
        loaded.CurrentStep.Should().Be(0);
    }

    [Fact]
    public async Task AddStep_ShouldRegisterStep()
    {
        var saga = new TestOrderSaga();
        await saga.DefineStepsAsync();

        saga.Steps.Should().HaveCount(1);
        saga.Steps[0].Name.Should().Be("CreateOrder");
        saga.Steps[0].EventType.Should().Be(typeof(OrderCreatedEvent));
    }

    [Fact]
    public async Task InMemorySagaStore_SaveAndLoad_ShouldPersist()
    {
        var store = new InMemorySagaStore();
        var state = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Test",
            Status = SagaStatus.Running,
            CurrentStep = 0
        };

        await store.SaveAsync(state);
        var loaded = await store.LoadAsync(state.SagaId);

        loaded.Should().NotBeNull();
        loaded!.SagaId.Should().Be(state.SagaId);
        loaded.Status.Should().Be(SagaStatus.Running);
    }

    [Fact]
    public async Task InMemorySagaStore_Update_ShouldModifyState()
    {
        var store = new InMemorySagaStore();
        var state = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Test",
            Status = SagaStatus.Running
        };
        await store.SaveAsync(state);

        state.Status = SagaStatus.Completed;
        await store.UpdateAsync(state);

        var loaded = await store.LoadAsync(state.SagaId);
        loaded!.Status.Should().Be(SagaStatus.Completed);
    }
}

public record OrderCreatedEvent(Guid CorrelationId) : IEvent
{
    public Guid SagaId => CorrelationId;
}

public record OtherTestEvent : IEvent;

public class TestOrderSaga : global::FlowCore.Saga.Saga
{
    public override Task DefineStepsAsync()
    {
        AddStep<OrderCreatedEvent>("CreateOrder",
            execute: (evt, ct) =>
            {
                return Task.CompletedTask;
            },
            compensate: (evt, ct) =>
            {
                return Task.CompletedTask;
            });
        return Task.CompletedTask;
    }
}

public class TestFailingSaga : global::FlowCore.Saga.Saga
{
    public override Task DefineStepsAsync()
    {
        AddStep<OrderCreatedEvent>("CreateOrder",
            execute: (evt, ct) =>
            {
                throw new InvalidOperationException("Step failed");
            },
            compensate: (evt, ct) =>
            {
                return Task.CompletedTask;
            });
        return Task.CompletedTask;
    }
}
