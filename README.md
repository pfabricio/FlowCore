# FlowCore

![NuGet Version](https://img.shields.io/nuget/v/FlowCore)
![NuGet Downloads](https://img.shields.io/nuget/dt/FlowCore)

**FlowCore** é um framework .NET 8+ para arquiteturas CQRS, Event-Driven e Microsserviços. Oferece um Mediator extensível com Pipeline Behaviors, EventBus com suporte a múltiplos providers de mensageria (RabbitMQ, Kafka, InMemory), além de padrões avançados como Outbox, Inbox, Saga Orchestration, Retry, Dead Letter, Scheduled Messages, OpenTelemetry, Execution Scope, Handler Discovery, Module System e Source Generator.

---

## ✨ Recursos

### CQRS + Pipeline
- **Commands**, **Queries** e **Events**
- Pipeline Behaviors: Logging, Validation (FluentValidation), Caching, Transações (EF Core), Event Dispatcher
- Handler Discovery centralizado com `IHandlerRegistry` e validação de duplicados
- Source Generator opcional para eliminar Reflection em compile-time

### EventBus
- `IEventBus` — abstração única para publicação de eventos
- `InMemoryEventBus` — provider padrão, sem Reflection, com delegates compilados
- Delegate Dispatcher com cache thread-safe Singleton
- `DiagnosticsEventBus` — decorator com tracing, métricas e `IDiagnosticsContext`
- Providers como `IMessageProvider` com ciclo de vida Start/Stop

### Providers de Mensageria
- **RabbitMQ** — `AddRabbitMQ()` com publish/consumer worker, reconexão automática
- **Kafka** — `AddKafka()` com publish/consumer groups, commit gerenciado
- Registrados via `IProviderRegistry` para descoberta em runtime

### Resiliência
- **Retry** — `IRetryPolicy` com políticas configuráveis (ImmediateRetry, ExponentialBackoff)
- **Dead Letter Queue** — `IDeadLetterWriter` para mensagens com falha permanente

### Padrões Transacionais
- **Outbox** — publicação confiável de eventos com `IOutboxStore` + `OutboxWorker`
- **Inbox** — processamento idempotente com `IInboxStore` (deduplicação por MessageId)

### Observabilidade
- **OpenTelemetry** — abstrações `IActivityFactory` + `IMetricRecorder` (no-op por padrão)
- **Diagnostics Context** — `IDiagnosticsContext` com `DiagnosticEntry` centralizado no `ExecutionScope`
- Tracing distribuído com propagação de CorrelationId

### Execution Scope
- `IExecutionScope` — contexto compartilhado por execução (CorrelationId, Items, Diagnostics)
- `AsyncLocal` thread-safe, disponível para Pipeline, EventBus, Behaviors e Providers

### Orquestração
- **Saga Orchestration** — `SagaCoordinator` com steps, compensação em ordem reversa, persistência de estado
- **Scheduled Messages** — agendamento absoluto/relativo com `IMessageScheduler` + `SchedulerWorker`

### Module System
- `IFlowCoreBuilder` — API fluente para registro de módulos
- `IFlowCoreModule` — interface para criação de módulos independentes
- `FlowCoreOptions` com validação via `IValidateOptions`

---

## 📦 Instalação

### Núcleo
```bash
dotnet add package FlowCore --version 2.1.0
```

### Providers
```bash
dotnet add package FlowCore.RabbitMQ --version 2.1.0
dotnet add package FlowCore.Kafka --version 2.1.0
```

---

## ⚙️ Configuração

### Configuração básica
```csharp
builder.Services.AddFlowCore();
```

### Com RabbitMQ
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

### Com Kafka
```csharp
builder.Services
    .AddFlowCore()
    .AddKafka(options =>
    {
        options.BootstrapServers = "localhost:9092";
        options.ConsumerGroup = "my-service";
    });
```

### Recursos opcionais
```csharp
builder.Services
    .AddFlowCore()
    .AddFlowCoreTransactions()      // EF Core transaction scope
    .AddFlowCoreOutbox()             // Outbox Worker
    .AddFlowCoreDiagnostics()        // System.Diagnostics Activity + Metrics
    .AddFlowCoreSagaListener()       // Saga event listener
    .AddFlowCoreScheduler();         // Scheduled Messages Worker
```

---

## 💡 Exemplo de Uso

### Commands e Queries
```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { Id = Guid.NewGuid(), Name = command.Name, Email = command.Email };
        // persist...
        return user.Id;
    }
}

var userId = await _mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));
```

### Eventos
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

### Mensagens Agendadas
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
            // executar
        }, compensate: async (evt, ct) =>
        {
            // compensar se falhar
        });

        AddStep<PaymentProcessedEvent>("ProcessPayment", async (evt, ct) =>
        {
            // executar
        });

        return Task.CompletedTask;
    }
}

// Registrar
builder.Services.AddSaga<OrderSaga>();
builder.Services.AddFlowCoreSagaListener();
```

---

## 🧪 Testes

21 testes unitários cobrindo Mediator, Behaviors, DI, EventBus, Serialização, Retry, DLQ, Outbox, Inbox, Tracing, Saga e Scheduling.

```bash
dotnet test
```

---

## 📚 Documentação

### Português (Brasil)
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

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se livre para abrir issues ou pull requests.

---

## 📄 Licença

MIT License
