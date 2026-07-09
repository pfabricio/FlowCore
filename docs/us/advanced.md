# Advanced — FlowCore

> Advanced topics: Module Manifest, Hosting & Lifecycle, Hosted Workers, Health Checks, Metrics, Resilience, Plugin Model, EventBus, Saga, Scheduling and Testing Infrastructure.

---

## 📖 Overview

This guide covers all advanced FlowCore features for distributed and resilient architectures.

---

## 🧩 Module Manifest

The Module Manifest represents the official identity of each FlowCore module.

### IModuleManifest

```csharp
public interface IModuleManifest
{
    string Name { get; }
    Version Version { get; }
    Version? MinimumFlowCoreVersion { get; }
    IReadOnlyCollection<string> Capabilities { get; }
    IReadOnlyCollection<Type> Dependencies { get; }
}
```

### IModuleRegistry

```csharp
public interface IModuleRegistry
{
    IReadOnlyCollection<IModuleManifest> Modules { get; }
}
```

### Creating a module with Manifest

```csharp
public class MyModule : IFlowCoreModule
{
    public IModuleManifest Manifest { get; }
        = new ModuleManifest(
            "MyModule",
            new Version(1, 0, 0),
            ["CustomProvider", "HealthCheck"],
            minimumFlowCoreVersion: new Version(2, 2, 0));

    public void Configure(IFlowCoreBuilder builder)
    {
        builder.AddHealthCheck<MyHealthCheck>();
        builder.AddHostedWorker<MyWorker>();
    }
}

builder.Services.AddFlowCore().AddModule<MyModule>();
```

Bootstrap automatically validates that the FlowCore version meets each module's `MinimumFlowCoreVersion`.

---

## 🏗️ Hosting & Application Lifecycle

Coordinates the ordered startup and shutdown of all components.

### BootstrapCoordinator

Bootstrap follows this order:

```
Core → Configuration → Providers → Workers → Application Ready
```

Shutdown happens in reverse order:

```
Workers → Providers → Core
```

### IBootstrapCoordinator

```csharp
public interface IBootstrapCoordinator
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
```

Automatically integrated with .NET Generic Host via `BootstrapHostedService`. Startup failures abort the application; shutdown failures never block other components.

---

## 🔄 Hosted Workers

Workers are components that execute continuous background processing. Each work unit has its own `ExecutionScope`.

### IHostedWorker

```csharp
public interface IHostedWorker
{
    string Name { get; }
    ValueTask StartAsync(CancellationToken cancellationToken = default);
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
```

### IHostedWorkerManager

```csharp
public interface IHostedWorkerManager
{
    IReadOnlyCollection<IHostedWorker> Workers { get; }
}
```

### Registration

```csharp
builder.Services.AddFlowCore()
    .AddHostedWorker<MyCustomWorker>();
```

Workers follow the lifecycle: Create → Start → Execute (loop) → Stop → Dispose. Each iteration creates and disposes an `ExecutionScope`.

---

## ❤️ Health Checks

Unified system for checking component operational status.

### IHealthCheck

```csharp
public interface IHealthCheck
{
    ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
```

### HealthCheckResult

```csharp
public sealed class HealthCheckResult
{
    public string Name { get; }
    public HealthStatus Status { get; } // Healthy, Degraded, Unhealthy
    public string? Description { get; }
    public TimeSpan Duration { get; }
    public Exception? Exception { get; }
    public IReadOnlyDictionary<string, object> Metadata { get; }

    public static HealthCheckResult Healthy(string name, string? description = null);
    public static HealthCheckResult Degraded(string name, string? description = null, Exception? exception = null);
    public static HealthCheckResult Unhealthy(string name, string? description = null, Exception? exception = null);
}
```

### Creating a Health Check

```csharp
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqConnectionManager _connection;

    public RabbitMqHealthCheck(RabbitMqConnectionManager connection)
    {
        _connection = connection;
    }

    public async ValueTask<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        try
        {
            var isConnected = await _connection.IsConnectedAsync(ct);
            return isConnected
                ? HealthCheckResult.Healthy("rabbitmq", "Connected")
                : HealthCheckResult.Degraded("rabbitmq", "Not connected");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("rabbitmq", "Connection failed", ex);
        }
    }
}

// Registration
builder.Services.AddFlowCore().AddHealthCheck<RabbitMqHealthCheck>();
```

`IHealthCheckRegistry` aggregates all registered checks and exposes the overall status (worst state wins).

---

## 📊 Metrics Context

Per-`ExecutionScope` metrics collection, enabling instrumentation of Pipeline, EventBus, Providers and Workers.

### IMetricsContext

```csharp
public interface IMetricsContext
{
    void Record(MetricEntry metric);
    IReadOnlyCollection<MetricEntry> Entries { get; }
}
```

### MetricEntry

```csharp
public sealed class MetricEntry
{
    public string Name { get; }
    public MetricType Type { get; }    // Counter, Gauge, Histogram, Timer
    public double Value { get; }
    public string? Unit { get; }
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyDictionary<string, string> Tags { get; }
}
```

### Usage in Pipeline

```csharp
public class MetricsBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    public async Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        var scope = ExecutionScope.Current;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            scope?.Metrics.Record(new MetricEntry(
                "handler.duration", MetricType.Timer, stopwatch.Elapsed.TotalMilliseconds,
                unit: "ms", tags: new Dictionary<string, string> { ["handler"] = typeof(TRequest).Name }));

            return result;
        }
        catch
        {
            scope?.Metrics.RecordCounter("handler.failures");
            throw;
        }
    }
}
```

Each `ExecutionScope` has its own `MetricsContext`. Metrics are isolated per execution and never shared.

---

## 🛡️ Resilience

Unified resilience policies under the `IResiliencePolicy` interface.

### IResiliencePolicy

```csharp
public interface IResiliencePolicy
{
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);
}
```

### TimeoutPolicy

Limits maximum execution time.

```csharp
var timeout = new TimeoutPolicy(TimeSpan.FromSeconds(5));
await policy.ExecuteAsync(ct => MyOperationAsync(ct), ct);
```

### CircuitBreakerPolicy

Protects against consecutive failures.

```csharp
var circuit = new CircuitBreakerPolicy(
    failureThreshold: 5,
    openDuration: TimeSpan.FromSeconds(30));

// States: Closed → Open → HalfOpen → Closed
```

### BulkheadPolicy

Limits the number of concurrent operations.

```csharp
var bulkhead = new BulkheadPolicy(maxConcurrency: 10);
```

### FallbackPolicy

Executes an alternative strategy on failure.

```csharp
var fallback = new FallbackPolicy(() => Console.WriteLine("Fallback executed"));
```

### RateLimiterPolicy

Controls execution rate.

```csharp
var rateLimiter = new RateLimiterPolicy(maxRequestsPerSecond: 100);
```

### PolicyComposer

Combines multiple policies into a pipeline.

```csharp
var pipeline = new PolicyComposer(
    new CircuitBreakerPolicy(failureThreshold: 3),
    new TimeoutPolicy(TimeSpan.FromSeconds(10)),
    new RetryPolicy(maxAttempts: 3));

var result = await pipeline.ExecuteAsync(ct => MyOperationAsync(ct), ct);
```

Execution order goes from first to last policy (outer → inner).

---

## 🔧 EventBus

### IEventBus

`IEventBus` is the central abstraction for distributed event publishing. The default provider is `InMemoryEventBus`. With `FlowCore.RabbitMQ` and `FlowCore.Kafka` packages, events can be published to external messaging systems.

```csharp
await _eventBus.PublishAsync(new OrderPlacedEvent(orderId, userId, total));

builder.Services.AddFlowCore().AddRabbitMQ(options => { ... });
```

### DispatcherCache

Handler resolution uses cached compiled delegates in a thread-safe `DispatcherCache` (Singleton), eliminating Reflection on the hot path. The Source Generator can generate the dispatcher at compile-time.

---

## 🔄 Resilience (Retry and DLQ)

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
await _scheduler.ScheduleAfterAsync(new OrderExpiredEvent(orderId), TimeSpan.FromHours(2));
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

## ⚙️ Configuration (FlowCoreOptions)

Global framework options configurable via `IConfigureOptions<FlowCoreOptions>` or `appsettings.json`.

```csharp
builder.Services.Configure<FlowCoreOptions>(options =>
{
    options.MaxRetryAttempts = 5;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(15);
});
```

Automatic validation via `IValidateOptions<FlowCoreOptions>`:
- `MaxRetryAttempts` must be >= 0
- `DefaultCacheExpiration` cannot be zero or negative

---

## 🔍 Handler Discovery

Centralized in `IHandlerRegistry` and `HandlerDiscovery`. The discovery is **hybrid**: it first tries to load `GeneratedHandlerRegistry` generated by the Source Generator at compile-time; if not found, it falls back to reflection.

```csharp
public interface IHandlerRegistry
{
    HandlerDescriptor? GetHandler(Type requestType);
    IReadOnlyCollection<HandlerDescriptor> GetEventHandlers(Type eventType);
}
```

Automatically registered as Singleton via `AddFlowCore()`.

### Source Generator (hybrid)

The `FlowCore.Generators` package (Roslyn Incremental Generator) generates:
- `GeneratedDispatcher.g.cs` — type-safe dispatch without reflection
- `GeneratedHandlerRegistry.g.cs` — compile-time handler registration
- `GeneratedDiRegistration.g.cs` — DI registration without assembly scanning

The runtime attempts to use generated code first. If unavailable (project without Source Generator), it falls back to reflection annotated with `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]`.

---

## 🏗️ Execution Scope

`IExecutionScope` is a shared context per execution, available without dependency injection via `AsyncLocal<ExecutionScope?>.Current`.

```csharp
public interface IExecutionScope : IDisposable
{
    Guid Id { get; }
    ICorrelationContext Correlation { get; }
    IExecutionItems Items { get; }
    IDiagnosticsContext Diagnostics { get; }
    IMetricsContext Metrics { get; }
    CancellationToken CancellationToken { get; }
}
```

Created automatically at the start of each `ExecutePipeline` and disposed at the end. Accessible from anywhere in the pipeline:

```csharp
var scope = ExecutionScope.Current;
scope?.Diagnostics.Write("step", "handler-executed");
scope?.Metrics.RecordCounter("requests.total");
```

---

## 🔌 Provider Registry

`IProviderRegistry` maintains the registry of all available `IMessageProvider` instances. The lifecycle (Start/Stop) is managed by `BootstrapCoordinator`.

```csharp
public interface IMessageProvider
{
    string Name { get; }
    ValueTask StartAsync(CancellationToken ct);
    ValueTask StopAsync(CancellationToken ct);
}
```

Implemented by `InMemoryEventBus`, `RabbitMqEventBus` and `KafkaEventBus`.

---

## 📊 Diagnostics Context

`IDiagnosticsContext` collects diagnostic entries during execution.

```csharp
public interface IDiagnosticsContext
{
    IReadOnlyList<DiagnosticEntry> Entries { get; }
    void Write(string step, string message, object? metadata = null);
}
```

Available via `ExecutionScope.Current.Diagnostics`.

---

## 🧩 Module System

`IFlowCoreBuilder` is the fluent API for FlowCore configuration, returned by `AddFlowCore()`.

```csharp
public interface IFlowCoreBuilder
{
    IServiceCollection Services { get; }
    IFlowCoreBuilder AddModule<T>() where T : IFlowCoreModule, new();
    IFlowCoreBuilder RegisterManifest(IModuleManifest manifest);
    IFlowCoreBuilder AddHealthCheck<T>() where T : class, IHealthCheck;
    IFlowCoreBuilder AddHostedWorker<T>() where T : class, IHostedWorker;
}
```

`IFlowCoreModule` allows creating independent modules with mandatory Manifest:

```csharp
public class MyModule : IFlowCoreModule
{
    public IModuleManifest Manifest { get; }
        = new ModuleManifest("MyModule", new Version(1, 0, 0), ["Service"]);

    public void Configure(IFlowCoreBuilder builder)
    {
        builder.Services.AddSingleton<IMyService, MyService>();
    }
}

builder.Services.AddFlowCore().AddModule<MyModule>();
```

---

## 🔌 Plugin Model

Plugins are third-party modules that follow exactly the same contracts as official modules.

### PluginModule

```csharp
public abstract class PluginModule : IFlowCoreModule
{
    public abstract IModuleManifest Manifest { get; }
    public abstract void Configure(IFlowCoreBuilder builder);

    protected static IModuleManifest CreateManifest(
        string name, Version version, Version minimumFlowCoreVersion,
        string[]? capabilities = null, Type[]? dependencies = null);
}
```

### Plugin Example

```csharp
public class RedisPlugin : PluginModule
{
    public override IModuleManifest Manifest { get; }
        = CreateManifest("FlowCore.Redis", new Version(1, 0, 0),
            new Version(2, 2, 0),
            capabilities: ["EventBusProvider", "HealthCheck"]);

    public override void Configure(IFlowCoreBuilder builder)
    {
        builder.AddHealthCheck<RedisHealthCheck>();
    }
}

builder.Services.AddFlowCore().AddModule<RedisPlugin>();
```

Bootstrap automatically validates version compatibility. Plugins can register Providers, Workers, Behaviors, Health Checks and Metrics — without any changes to the Core.

---

## 🧪 Testing Infrastructure

The `FlowCore.Testing` package provides infrastructure for testing FlowCore-based applications.

### FakeEventBus

```csharp
public sealed class FakeEventBus : IEventBus
{
    public IReadOnlyCollection<object> Published { get; }
    public IReadOnlyCollection<TEvent> PublishedOfType<TEvent>() where TEvent : IEvent;
    public void Clear();
}
```

### FlowCoreTestBuilder

```csharp
var services = new ServiceCollection();
var builder = services.CreateTestBuilder(); // AddFlowCore + FakeEventBus
var provider = builder.Build();

var mediator = provider.GetRequiredService<IFlowMediator>();
var fakeBus = provider.GetFakeEventBus();

// Execute scenario
var result = await mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));

// Verify published events
Assert.Single(fakeBus.Published);
Assert.IsType<UserCreatedEvent>(fakeBus.Published.Single());
```

### FakeClock

```csharp
var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
clock.Advance(TimeSpan.FromHours(2));
clock.Set(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
```

---

## 📝 Best Practices

1. **Use Outbox** to guarantee delivery of critical events
2. **Configure Retry + DLQ** for resilience against transient failures
3. **Use Inbox** in consumers for idempotent processing
4. **Model Sagas** for distributed transactions with compensation
5. **Enable Diagnostics** for production observability
6. **Schedule messages** for deferred logic without external cron jobs
7. **Define Module Manifests** for all modules and plugins
8. **Register Health Checks** for operational monitoring
9. **Use FlowCore.Testing** for tests without external infrastructure
10. **Prefer Source Generators** for Native AOT compatibility
