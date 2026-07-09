# Advanced — FlowCore

> Advanced topics: EventBus, Retry, DLQ, Outbox, Inbox, Saga, Scheduled Messages, Observability, Configuration, Handler Discovery, Execution Scope, Provider Bootstrap, Diagnostics Context, Module System and Source Generator.

---

## 📖 Overview

This guide covers FlowCore's advanced features for distributed and resilient architectures.

---

## 🔧 EventBus

### IEventBus

`IEventBus` is the central abstraction for distributed event publishing. The default provider is `InMemoryEventBus`. With `FlowCore.RabbitMQ` and `FlowCore.Kafka` packages, events can be published to external messaging systems.

```csharp
// Publish event
await _eventBus.PublishAsync(new OrderPlacedEvent(orderId, userId, total));

// Provider is resolved via DI based on configuration
builder.Services.AddFlowCore().AddRabbitMQ(options => { ... });
```

### DispatcherCache

Handler resolution uses cached compiled delegates in a thread-safe `DispatcherCache` (Singleton), eliminating Reflection on the hot path.

---

## 🔄 Resilience

### Retry Policy

`IRetryPolicy` defines the retry strategy for failed messages.

```csharp
public class ImmediateRetryPolicy : IRetryPolicy
{
    public RetryDecision Evaluate(RetryContext context) =>
        context.Attempt < 3
            ? RetryDecision.Retry(TimeSpan.Zero)
            : RetryDecision.Fail();
}
```

### Dead Letter Queue

Messages that exceed the retry limit are sent to DLQ via `IDeadLetterWriter`.

```csharp
public class InMemoryDeadLetterWriter : IDeadLetterWriter
{
    public Task WriteAsync(DeadLetterContext context, CancellationToken ct)
    {
        // persist message for later analysis
    }
}
```

---

## 📦 Outbox

The Outbox pattern guarantees reliable event publishing: the message is saved in the store along with the handler transaction, and an `OutboxWorker` (BackgroundService) publishes to EventBus in the background.

```csharp
builder.Services.AddFlowCore().AddFlowCoreOutbox();
```

Components:
- `IOutboxStore` — message storage (InMemory or custom)
- `OutboxMessage` — entity with Id, Type, Data, Status and Timestamp
- `OutboxWorker` — processes pending messages in a loop

---

## 📥 Inbox

The Inbox pattern guarantees idempotent processing: received messages are checked by `MessageId` before processing.

The check happens automatically in `ConsumerWorker.ProcessEnvelopeAsync`.

```csharp
public interface IInboxStore
{
    Task<bool> IsMessageProcessedAsync(string messageId, CancellationToken ct);
    Task MarkAsProcessedAsync(InboxMessage message, CancellationToken ct);
}
```

---

## 🎭 Saga

Saga Orchestration support for coordinating distributed transactions with compensation.

```csharp
public class OrderSaga : Saga
{
    public override Task DefineStepsAsync()
    {
        AddStep<OrderPlacedEvent>("ReserveInventory", async (evt, ct) =>
        {
            // main logic
        }, compensate: async (evt, ct) =>
        {
            // compensation logic (reverse order)
        });

        AddStep<PaymentProcessedEvent>("ProcessPayment", async (evt, ct) =>
        {
            // next step
        });

        return Task.CompletedTask;
    }
}

// Register
builder.Services.AddSaga<OrderSaga>();
builder.Services.AddFlowCoreSagaListener();
```

Components:
- `SagaCoordinator` — orchestrates steps and compensation
- `ISagaStore` — persists saga state
- `SagaEventListener` — listens to events and triggers steps

---

## ⏰ Scheduled Messages

Schedule messages for future publication (absolute or relative).

```csharp
// Schedule for 2 hours from now
await _scheduler.ScheduleAfterAsync(new OrderExpiredEvent(orderId), TimeSpan.FromHours(2));

// Schedule for specific date
await _scheduler.ScheduleAtAsync(new ReminderEvent(userId), DateTimeOffset.UtcNow.AddDays(1));
```

```csharp
builder.Services.AddFlowCore().AddFlowCoreScheduler();
```

Components:
- `IMessageScheduler` — scheduling interface
- `IScheduledMessageStore` — persisted scheduled messages
- `SchedulerWorker` — BackgroundService that publishes messages on time

---

## 📊 Observability

### DiagnosticsEventBus

Decorator that adds tracing and metrics to the EventBus. No-op by default (zero overhead); enabled with:

```csharp
builder.Services.AddFlowCore().AddFlowCoreDiagnostics();
```

### Activity & Metrics

- `IActivityFactory` / `SystemDiagnosticsActivityFactory` — creates `Activity` for distributed tracing
- `IMetricRecorder` / `SystemDiagnosticsMetricRecorder` — records metrics via `System.Diagnostics.Metrics`
- CorrelationId is propagated in the message envelope

---

---

## ⚙️ Configuration (FlowCoreOptions)

Global framework options configurable via `IConfigureOptions<FlowCoreOptions>` or `appsettings.json`.

```csharp
builder.Services.Configure<FlowCoreOptions>(options =>
{
    options.MaxRetryAttempts = 5;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(15);
});
```

Validation is automatic via `IValidateOptions<FlowCoreOptions>`:
- `MaxRetryAttempts` must be >= 0
- `DefaultCacheExpiration` cannot be zero or negative

---

## 🔍 Handler Discovery

Centralized in `IHandlerRegistry` — replaces scattered assembly scanning with a single registry with duplicate detection.

```csharp
public interface IHandlerRegistry
{
    IReadOnlyList<HandlerDescriptor> GetHandlers(Type handlerType);
    bool IsHandlerRegistered(Type handlerType, Type implementationType);
}
```

Registered automatically as Singleton via `AddFlowCore()`. During bootstrap, `HandlerDiscovery` scans assemblies and registers all handlers implementing `ICommandHandler<>`, `IQueryHandler<>` and `IEventHandler<>`.

### Source Generator (optional)

The `FlowCore.Generators` package (Roslyn Incremental Generator) generates `HandlerRegistry`, `DiRegistration` and `Dispatcher` at compile-time, eliminating Reflection. The runtime attempts to use generated code first, falling back to Reflection-based discovery.

---

## 🏗️ Execution Scope

`IExecutionScope` is a shared context per execution, available without dependency injection via `AsyncLocal<ExecutionScope?>.Current`.

```csharp
public interface IExecutionScope : IDisposable
{
    string CorrelationId { get; }
    IExecutionItems Items { get; }
    IDiagnosticsContext Diagnostics { get; }
}
```

Created automatically at the start of each `ExecutePipeline` and disposed at the end. Accessible from anywhere in the pipeline: Behaviors, Handlers, EventBus and Providers.

```csharp
// Anywhere in the pipeline (no DI required)
var scope = ExecutionScope.Current;
scope?.Diagnostics.Write("step", "handler-executed");
```

---

## 🔌 Provider Registry

`IProviderRegistry` maintains the registry of all available `IMessageProvider` instances, enabling runtime discovery.

```csharp
public interface IProviderRegistry
{
    IReadOnlyList<IMessageProvider> GetProviders();
    void Register(IMessageProvider provider);
}
```

`IMessageProvider` defines a common lifecycle contract:

```csharp
public interface IMessageProvider
{
    string Name { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
```

Implemented by `InMemoryEventBus`, `RabbitMqEventBus` and `KafkaEventBus`.

---

## 📊 Diagnostics Context

`IDiagnosticsContext` collects diagnostic entries during operation execution.

```csharp
public interface IDiagnosticsContext
{
    IReadOnlyList<DiagnosticEntry> Entries { get; }
    void Write(string step, string message, object? metadata = null);
}
```

Available via `ExecutionScope.Current.Diagnostics`. `DiagnosticsEventBus` and `ConsumerWorker.ProcessEnvelopeAsync` already write entries automatically.

### DiagnosticEntry

```csharp
public record DiagnosticEntry(
    string Step,
    string Message,
    object? Metadata,
    DateTimeOffset Timestamp);
```

---

## 🧩 Module System

`IFlowCoreBuilder` is the fluent API for FlowCore configuration, returned by `AddFlowCore()`.

```csharp
public interface IFlowCoreBuilder
{
    IServiceCollection Services { get; }
    IFlowCoreBuilder AddModule<T>() where T : IFlowCoreModule, new();
}
```

`IFlowCoreModule` allows creating independent modules:

```csharp
public class MyModule : IFlowCoreModule
{
    public void Configure(IFlowCoreBuilder builder)
    {
        builder.Services.AddSingleton<IMyService, MyService>();
    }
}

// Usage
builder.Services.AddFlowCore().AddModule<MyModule>();
```

RabbitMQ and Kafka providers extend `IFlowCoreBuilder` instead of `IServiceCollection`.

---

## 📝 Best Practices

1. **Use Outbox** to guarantee delivery of critical events
2. **Configure Retry + DLQ** for resilience against transient failures
3. **Use Inbox** in consumers for idempotent processing
4. **Model Sagas** for distributed transactions with compensation
5. **Enable Diagnostics** for production observability
6. **Schedule messages** for deferred logic without external cron jobs
