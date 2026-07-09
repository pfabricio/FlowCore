# 📦 FlowCore

**FlowCore** is a .NET 8+ framework for CQRS, Event-Driven, and Microservices architectures. It provides an extensible Mediator with Pipeline Behaviors, an EventBus with multiple messaging providers (RabbitMQ, Kafka, InMemory), plus Outbox, Inbox, Saga Orchestration, Retry, Dead Letter, Scheduled Messages, OpenTelemetry, Execution Scope, Handler Discovery, Module System and Source Generator.

## 🎯 Features

### CQRS + Pipeline
- **Commands**, **Queries**, and **Events**
- Pipeline Behaviors: Logging, Validation (FluentValidation), Caching, Transactions (EF Core), Event Dispatcher
- Centralized `IHandlerRegistry` with duplicate detection
- Optional Source Generator to eliminate Reflection at compile-time

### EventBus
- `IEventBus` — single abstraction for event publishing
- `InMemoryEventBus` — default provider, reflection-free compiled delegates
- Thread-safe `DispatcherCache` (Singleton)
- `DiagnosticsEventBus` — decorator with tracing, metrics and `IDiagnosticsContext`
- Providers as `IMessageProvider` with Start/Stop lifecycle

### Messaging Providers
- **RabbitMQ** — `AddRabbitMQ()` with publish/consumer worker, auto-reconnect
- **Kafka** — `AddKafka()` with publish/consumer groups, managed commits
- Registered via `IProviderRegistry` for runtime discovery

### Resilience
- **Retry** — `IRetryPolicy` with configurable strategies (Immediate, ExponentialBackoff)
- **Dead Letter Queue** — `IDeadLetterWriter` for permanently failed messages

### Transactional Patterns
- **Outbox** — reliable event publishing with `IOutboxStore` + `OutboxWorker`
- **Inbox** — idempotent processing with `IInboxStore` (deduplication by MessageId)

### Observability
- **OpenTelemetry** — `IActivityFactory` + `IMetricRecorder` (no-op by default)
- **Diagnostics Context** — `IDiagnosticsContext` with `DiagnosticEntry` central in `ExecutionScope`
- Distributed tracing with CorrelationId propagation

### Execution Scope
- `IExecutionScope` — shared context per execution (CorrelationId, Items, Diagnostics)
- Thread-safe `AsyncLocal`, available to Pipeline, EventBus, Behaviors and Providers

### Orchestration
- **Saga** — `SagaCoordinator` with steps, reverse-order compensation, state persistence
- **Scheduled Messages** — absolute/relative scheduling with `IMessageScheduler` + `SchedulerWorker`

### Module System
- `IFlowCoreBuilder` — fluent API for module registration
- `IFlowCoreModule` — interface for independent module creation
- `FlowCoreOptions` with validation via `IValidateOptions`

## 📥 Installation

### Core
```bash
dotnet add package FlowCore --version 2.1.0
```

### Providers
```bash
dotnet add package FlowCore.RabbitMQ --version 2.1.0
dotnet add package FlowCore.Kafka --version 2.1.0
```

## ⚙️ Configuration

### Basic
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
    .AddFlowCoreTransactions()
    .AddFlowCoreOutbox()
    .AddFlowCoreDiagnostics()
    .AddFlowCoreSagaListener()
    .AddFlowCoreScheduler();
```

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
            // compensate if later step fails
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

## ✅ Tests

21 unit tests covering Mediator, Behaviors, DI, EventBus, Serialization, Retry, DLQ, Outbox, Inbox, Tracing, Saga, and Scheduling.

```bash
dotnet test
```

## 📄 License

MIT License
