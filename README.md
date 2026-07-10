# FlowCore

![NuGet Version](https://img.shields.io/nuget/v/FlowCore)
![NuGet Downloads](https://img.shields.io/nuget/dt/FlowCore)

**FlowCore** is a .NET 8+ framework for CQRS, Event-Driven and Microservices architectures. It provides an extensible Mediator with Pipeline Behaviors, multi-provider EventBus (RabbitMQ, Kafka, InMemory), Outbox, Inbox, Saga, Scheduling, Retry, DLQ, OpenTelemetry, Execution Scope, Handler Discovery (hybrid: Source Generator + reflection fallback), Module Manifest, Health Checks, Metrics Context, Resilience Policies, Hosted Workers, Plugin Model and Testing Infrastructure.

---

## ✨ Features

### CQRS + Pipeline
- **Commands**, **Queries** and **Events**
- Pipeline Behaviors: Logging, Validation (FluentValidation), Caching, Transactions (EF Core), Event Dispatcher
- Hybrid Handler Discovery: `GeneratedHandlerRegistry` (compile-time) with reflection fallback
- Optional Source Generator to eliminate Runtime Reflection

### EventBus
- `IEventBus` — single abstraction for event publishing
- Providers: **InMemory** (default), **RabbitMQ**, **Kafka**
- `DiagnosticsEventBus` — decorator with tracing, metrics and `IDiagnosticsContext`
- Providers as `IMessageProvider` with Start/Stop lifecycle managed by `BootstrapCoordinator`

### Module Manifest
- `IModuleManifest` — official identity of each module (Name, Version, Capabilities, Dependencies)
- `IModuleRegistry` — central catalog of all loaded modules
- `PluginModule` — base class for third-party plugins
- Automatic version compatibility validation on Bootstrap

### Hosting & Application Lifecycle
- `IBootstrapCoordinator` — ordered startup and shutdown (Core → Providers → Workers)
- `BootstrapHostedService` — integration with .NET Generic Host
- Reverse shutdown order: Workers → Providers → Core
- Startup failures abort initialization; shutdown failures never block

### Hosted Workers
- `IHostedWorker` — unified interface for continuous processing workers
- `IHostedWorkerManager` — manages all workers lifecycle
- Each work unit creates a new `ExecutionScope` (mandatory isolation)
- Supports: RabbitMQ Consumer, Kafka Consumer, Outbox, Inbox, Scheduler, Dead Letter

### Health Checks
- `IHealthCheck` — component health verification interface
- `HealthCheckResult` with Status (Healthy, Degraded, Unhealthy), Duration, Metadata
- `IHealthCheckRegistry` — centralized check registration
- ASP.NET Core Health Checks integration via `AddHealthCheck<T>()`
- Each module registers only its own checks

### Metrics Context
- `IMetricsContext` — per-`ExecutionScope` metrics collection
- `MetricEntry` with Name, Type (Counter, Gauge, Histogram, Timer), Value, Tags
- Pipeline, EventBus, Providers, Retry and Scheduler can record metrics
- Isolated context per execution — never shared between executions

### Resilience
- `IResiliencePolicy` — unified abstraction for resilience policies
- Policies: **Timeout**, **Circuit Breaker**, **Bulkhead**, **Fallback**, **Rate Limiter**
- `PolicyComposer` — chained composition of multiple policies
- Integration with Pipeline, EventBus and Providers
- Existing `IRetryPolicy` + `ImmediateRetryPolicy`

### Messaging Providers
- **RabbitMQ** — `AddRabbitMQ()` with publish/consumer worker, auto-reconnect
- **Kafka** — `AddKafka()` with publish/consumer groups, managed commit
- Registered via `IProviderRegistry` + lifecycle managed by Bootstrap

### Transactional Patterns
- **Outbox** — reliable publishing with `IOutboxStore` + `OutboxWorker`
- **Inbox** — idempotent processing with `IInboxStore` (deduplication by MessageId)
- **Saga Orchestration** — `SagaCoordinator` with steps, reverse-order compensation
- **Scheduled Messages** — absolute/relative scheduling with `IMessageScheduler`

### Observability
- `IActivityFactory` + `IMetricRecorder` (no-op by default, zero overhead)
- `IDiagnosticsContext` with `DiagnosticEntry` centralized in `ExecutionScope`
- Distributed tracing with CorrelationId propagation

### Execution Scope
- `IExecutionScope` — shared context per execution (CorrelationId, Items, Diagnostics, **Metrics**)
- `AsyncLocal` thread-safe, available without DI in Pipeline, Behaviors, Handlers and Providers

### Plugin Model
- `PluginModule` — base class for plugins extending FlowCore
- Mandatory `ModuleManifest` with `MinimumFlowCoreVersion` for validation
- Plugins can register: Providers, Workers, Behaviors, Health Checks, Metrics
- Bootstrap treats plugins and official modules identically

### AOT Compatibility
- Preferred path via Source Generators (zero runtime reflection)
- Reflection fallback annotated with `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]`
- `FlowMediator`, `HandlerDiscovery` and `DispatcherCache` prioritize generated code
- Ready for Native AOT and Linker Trimming

### Testing Infrastructure
- `FlowCore.Testing` — NuGet package with `FakeEventBus`, `FakeClock`, `IFlowCoreTestBuilder`
- Test environment setup without external infrastructure
- Isolation via `ExecutionScope` identical to runtime

---

## 📦 Installation

### Core
```bash
dotnet add package FlowCore --version 2.2.3
```

### Providers
```bash
dotnet add package FlowCore.RabbitMQ --version 2.2.3
dotnet add package FlowCore.Kafka --version 2.2.3
```

### Testing
```bash
dotnet add package FlowCore.Testing --version 2.2.3
```

---

## ⚙️ Configuration

### Basic setup
```csharp
builder.Services.AddFlowCore();
```

### With RabbitMQ
```csharp
builder.Services
    .AddFlowCore()
    .AddRabbitMQ(options =>
    {
        options.Host = "localhost";
        options.Username = "guest";
        options.Password = "guest";
    });
```

### With Kafka
```csharp
builder.Services
    .AddFlowCore()
    .AddKafka(options =>
    {
        options.BootstrapServers = "localhost:9092";
        options.ConsumerGroup = "my-service";
    });
```

### Optional modules
```csharp
builder.Services
    .AddFlowCore()
    .AddFlowCoreTransactions()      // EF Core transaction scope
    .AddFlowCoreOutbox()             // Outbox Worker
    .AddFlowCoreDiagnostics()        // System.Diagnostics Activity + Metrics
    .AddFlowCoreSagaListener()       // Saga event listener
    .AddFlowCoreScheduler();         // Scheduled Messages Worker
```

### Custom module with Manifest
```csharp
public class MyModule : IFlowCoreModule
{
    public IModuleManifest Manifest { get; }
        = new ModuleManifest("MyModule", new Version(1, 0, 0),
            ["CustomProvider", "HealthCheck"]);

    public void Configure(IFlowCoreBuilder builder)
    {
        builder.AddHealthCheck<MyHealthCheck>();
        builder.AddHostedWorker<MyWorker>();
    }
}

builder.Services.AddFlowCore().AddModule<MyModule>();
```

### Custom Health Check
```csharp
public class MyHealthCheck : IHealthCheck
{
    public async ValueTask<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        return HealthCheckResult.Healthy("my-component", "All ok");
    }
}
```

### Resilience policy
```csharp
var pipeline = new PolicyComposer(
    new CircuitBreakerPolicy(failureThreshold: 3),
    new TimeoutPolicy(TimeSpan.FromSeconds(5)));
```

---

## 💡 Usage Examples

### Commands and Queries
```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { Id = Guid.NewGuid(), Name = command.Name, Email = command.Email };
        return user.Id;
    }
}

var userId = await _mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));
```

### Events
```csharp
public record UserCreatedEvent(Guid UserId) : IEvent;

public class UserCreatedHandler : IEventHandler<UserCreatedEvent>
{
    public Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"User created: {@event.UserId}");
        return Task.CompletedTask;
    }
}

await _eventBus.PublishAsync(new UserCreatedEvent(userId));
```

### Scheduled Messages
```csharp
await _scheduler.ScheduleAfterAsync(
    new OrderExpiredEvent(orderId),
    TimeSpan.FromHours(2));
```

### Saga
```csharp
public class OrderSaga : Saga
{
    public override Task DefineStepsAsync()
    {
        AddStep<OrderPlacedEvent>("ReserveInventory", async (evt, ct) =>
        {
            // execute
        }, compensate: async (evt, ct) =>
        {
            // compensate on failure
        });

        AddStep<PaymentProcessedEvent>("ProcessPayment", async (evt, ct) =>
        {
            // execute
        });

        return Task.CompletedTask;
    }
}

builder.Services.AddSaga<OrderSaga>();
builder.Services.AddFlowCoreSagaListener();
```

---

## 🧪 Testing

131 unit tests covering Mediator, Behaviors, DI, EventBus, Serialization, Retry, DLQ, Outbox, Inbox, Tracing, Saga, Scheduling, DispatcherCache, Pipeline Integration, and Hosting.

```bash
dotnet test
```

For testing applications that use FlowCore, use the `FlowCore.Testing` package:

```csharp
var services = new ServiceCollection();
var builder = services.CreateTestBuilder();
var provider = builder.Build();
var fakeBus = provider.GetFakeEventBus();

// Execute scenario...

Assert.Single(fakeBus.Published);
```

---

## 📚 Documentation

### English (US)
- [Overview](docs/us/index.md)
- [Getting Started](docs/us/getting-started.md)
- [Commands](docs/us/commands.md)
- [Queries](docs/us/queries.md)
- [Events](docs/us/events.md)
- [Pipeline](docs/us/pipeline.md)
- [Cache](docs/us/cache.md)
- [Validation](docs/us/validation.md)
- [Authorization](docs/us/authorization.md)
- [Logging](docs/us/logging.md)
- [Transactions](docs/us/transactions.md)
- [Dependency Injection](docs/us/dependency-injection.md)
- [Testing](docs/us/testing.md)
- [Advanced](docs/us/advanced.md)

### Portuguese (Brazil)
- [Visão Geral](docs/pt-br/index.md)
- [Getting Started](docs/pt-br/getting-started.md)
- [Commands](docs/pt-br/commands.md)
- [Queries](docs/pt-br/queries.md)
- [Events](docs/pt-br/events.md)
- [Pipeline](docs/pt-br/pipeline.md)
- [Cache](docs/pt-br/cache.md)
- [Validation](docs/pt-br/validation.md)
- [Authorization](docs/pt-br/authorization.md)
- [Logging](docs/pt-br/logging.md)
- [Transactions](docs/pt-br/transactions.md)
- [Dependency Injection](docs/pt-br/dependency-injection.md)
- [Testing](docs/pt-br/testing.md)
- [Advanced](docs/pt-br/advanced.md)

---

## 🤝 Contributing

Contributions are welcome! Feel free to open issues or pull requests.

---

## 📄 License

MIT License
