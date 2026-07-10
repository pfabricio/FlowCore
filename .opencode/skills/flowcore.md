---
name: flowcore
description: Use when working with FlowCore framework code — creating/modifying commands, queries, events, sagas, pipelines, scheduling, resilience, source generators, or tests.
---

# FlowCore v2.2.3 — opencode Skill

## 1. Visão Geral

FlowCore é um framework .NET 8+ para arquiteturas CQRS, Event-Driven e Microservices.
- **Linguagem:** C# 12
- **Testes:** xUnit + FluentAssertions + Moq
- **Build:** `dotnet build` / `dotnet test`
- **Repositório:** `https://github.com/pfabricio/FlowCore`

## 2. Estrutura de Diretórios

| Diretório | Finalidade |
|-----------|------------|
| `FlowCore/` | Biblioteca principal (Mediator, Pipeline, EventBus, Saga, Scheduling, Resilience, etc.) |
| `FlowCore/Abstractions/` | `IFlowCoreBuilder`, `IFlowCoreModule`, `PluginModule` |
| `FlowCore/Configuration/` | `FlowCoreOptions`, `FlowCoreOptionsValidator` |
| `FlowCore/Core/Interfaces/` | Todas as interfaces públicas (23 interfaces) |
| `FlowCore/Core/` | `ModuleManifest`, `ModuleRegistry`, `Unit` |
| `FlowCore/Diagnostics/` | `DiagnosticsContext`, `MetricsContext`, Activity/Metrics factories |
| `FlowCore/Discovery/` | `HandlerDiscovery`, `HandlerRegistry`, `HandlerDescriptor` |
| `FlowCore/Execution/` | `ExecutionScope`, `ICorrelationContext`, `IExecutionItems` |
| `FlowCore/Extensions/` | `ServiceCollectionExtensions` (ponto de entrada `AddFlowCore()`) |
| `FlowCore/Hosting/` | `BootstrapCoordinator`, `BootstrapHostedService`, `HostedWorkerManager`, `IHostedWorker` |
| `FlowCore/Messaging/` | EventBus, Outbox, Inbox, Retry, DeadLetter, Serializer, ConsumerWorker |
| `FlowCore/Pipeline/Behaviors/` | Behaviors pré-construídos (Logging, Validation, Caching, EventDispatcher, TransactionScope) |
| `FlowCore/Resilience/` | CircuitBreaker, Bulkhead, RateLimiter, Timeout, Fallback, PolicyComposer |
| `FlowCore/Saga/` | Saga, SagaCoordinator, SagaState, SagaStep, SagaEventListener, ISagaStore |
| `FlowCore/Scheduling/` | MessageScheduler, SchedulerWorker, ScheduledMessage, IScheduledMessageStore |
| `FlowCore.Tests/` | Testes unitários |
| `FlowCore.Tests/Helpers/` | `TestCommand`, `TestQuery`, `TestEvent`, `TestCommandHandler`, etc. |
| `FlowCore.Generators/` | Source generators (Dispatcher, HandlerRegistry, DiRegistration) |
| `FlowCore.RabbitMQ/` | RabbitMQ EventBus + ConsumerWorker |
| `FlowCore.Kafka/` | Kafka EventBus + ConsumerWorker |
| `FlowCore.Testing/` | `FakeEventBus`, `FakeClock`, `FlowCoreTestBuilder` |

## 3. Ponto de Entrada

```csharp
// Program.cs
builder.Services.AddFlowCore();                        // scan all assemblies
// ou
builder.Services.AddFlowCore(typeof(MyHandler).Assembly);  // scan específico
```

Retorna `IFlowCoreBuilder` para configurar recursos adicionais.

## 4. CQRS — Commands

```csharp
// Command
public record CreateOrderCommand(string CustomerId, decimal Amount) : ICommand<Guid>;

// Handler
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        // lógica...
        return Guid.NewGuid();
    }
}

// Envio
var orderId = await mediator.SendAsync(new CreateOrderCommand("C1", 150));
```

Command sem retorno:
```csharp
public record DeleteOrderCommand(Guid OrderId) : ICommand<Unit>;
// handler retorna Task<Unit> — usar Unit.Value
```

## 5. CQRS — Queries

```csharp
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;
public class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> HandleAsync(GetOrderQuery query, CancellationToken ct)
    {
        // lógica...
    }
}

// Envio
var order = await mediator.QueryAsync(new GetOrderQuery(id));
```

## 6. Eventos

```csharp
public record OrderCreatedEvent(Guid OrderId, string CustomerId) : IEvent;

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        // lógica...
    }
}

// Publicação manual
await mediator.PublishAsync(new OrderCreatedEvent(orderId, "C1"));

// Ou automática via IEventSource (domain events)
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>, IEventSource
{
    public IEnumerable<IEvent> Events => _events;
    private readonly List<IEvent> _events = new();

    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        _events.Add(new OrderCreatedEvent(id, command.CustomerId));
        return id;
    }
}
// EventDispatcherBehavior publica automaticamente após o handler
```

## 7. Pipeline Behaviors

Os 4 behaviors default (executam em ordem):
1. `LoggingBehavior<,>` — loga nome do request + duração
2. `ValidationBehavior<,>` — executa `IValidator<TRequest>` do FluentValidation
3. `CachingBehavior<,>` — cache para queries que implementam `ICachableQuery<TResult>`
4. `EventDispatcherBehavior<,>` — publica `IEventSource.Events` após handler

Behavior customizado:
```csharp
public class MyBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    public async Task<TResult> Handle(TRequest request,
        RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        // before
        var result = await next();
        // after
        return result;
    }
}
// Registrar:
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MyBehavior<,>));
```

TransactionScope (opt-in):
```csharp
builder.AddFlowCoreTransactions();
```

## 8. Saga

```csharp
public class OrderSaga : Saga
{
    public override Task DefineStepsAsync()
    {
        AddStep<OrderCreatedEvent>("CreateOrder",
            execute: async (evt, ct) => { /* processa */ },
            compensate: async (evt, ct) => { /* rollback */ });
        AddStep<PaymentProcessedEvent>("ProcessPayment",
            execute: async (evt, ct) => { /* processa */ },
            compensate: async (evt, ct) => { /* rollback */ });
        return Task.CompletedTask;
    }
}

// Registrar
builder.AddSaga<OrderSaga>();
builder.AddFlowCoreSagaListener();

// Iniciar
var sagaId = await coordinator.StartAsync<OrderSaga>(new SagaState
{
    CorrelationId = correlationId
});
```

## 9. Scheduling

```csharp
// Agendar
await scheduler.ScheduleAsync(new OrderCreatedEvent(orderId, "C1"),
    DateTimeOffset.UtcNow.AddHours(1));

// Agendar com delay
await scheduler.ScheduleAfterAsync(new OrderCreatedEvent(orderId, "C1"),
    TimeSpan.FromHours(1));

// Ativar worker
builder.AddFlowCoreScheduler();
```

## 10. Resilience

```csharp
// Circuit Breaker
var circuit = new CircuitBreakerPolicy(5, TimeSpan.FromSeconds(30));
await circuit.ExecuteAsync(async ct => await SomeOperation(ct), ct);

// Bulkhead
var bulkhead = new BulkheadPolicy(10);
await bulkhead.ExecuteAsync(async ct => await SomeOperation(ct), ct);

// Rate Limiter
var rateLimiter = new RateLimiterPolicy(100); // 100 req/s
await rateLimiter.ExecuteAsync(async ct => await SomeOperation(ct), ct);

// Timeout
var timeout = new TimeoutPolicy(TimeSpan.FromSeconds(5));
await timeout.ExecuteAsync(async ct => await SomeOperation(ct), ct);

// Fallback
var fallback = new FallbackPolicy(() => Console.WriteLine("Fallback"));
await fallback.ExecuteAsync(async ct => await SomeOperation(ct), ct);

// Compor
var composed = new PolicyComposer(circuit, bulkhead, timeout);
await composed.ExecuteAsync(async ct => await SomeOperation(ct), ct);
```

## 11. Outbox

```csharp
builder.AddFlowCoreOutbox();  // registra OutboxWorker (BackgroundService)
```
Store padrão é `InMemoryOutboxStore`. Substituir implementando `IOutboxStore`.

## 12. Diagnostics

```csharp
builder.AddFlowCoreDiagnostics();
// Substitui NullActivityFactory/NullMetricRecorder por implementações reais
// com System.Diagnostics.Activity + métricas
```

## 13. Caching

```csharp
public record GetProductQuery(Guid Id) : ICachableQuery<ProductDto>
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

// Registrar ICacheProvider (ex.: Redis)
services.AddSingleton<ICacheProvider, RedisCacheProvider>();
```

## 14. Source Generators

Os generators são ativados automaticamente quando o projeto referencia `FlowCore`:
- **DispatcherGenerator** — gera `GeneratedDispatcher` com dispatch tipado sem reflection
- **HandlerRegistryGenerator** — gera `GeneratedHandlerRegistry` para discovery em compile-time
- **DiRegistrationGenerator** — gera `GeneratedDiRegistration` com registros DI

Não requer configuração adicional. Se o generated type não existir, o fallback usa reflection.

## 15. NativeAOT

Anotações necessárias em código que usa reflection:
- `[RequiresDynamicCode]` — em métodos que usam `MakeGenericType`, `Activator.CreateInstance`, `MethodInfo.Invoke`
- `[RequiresUnreferencedCode]` — em métodos que usam `GetTypes()`, `GetMethods()`, etc.

O caminho source-generated não precisa dessas anotações.
`DispatcherCache`, `HandlerDiscovery.DiscoverWithReflection`, `InvokeHandleWithReflectionAsync` já estão anotados.

## 16. Testes

```csharp
dotnet test                         # rodar todos os testes
dotnet test --filter "SagaTests"   # filtrar por suite
```

Fixture disponível:
```csharp
using var builder = new FlowCoreTestBuilder();
builder.Services.AddScoped<ICommandHandler<MyCommand, string>, MyHandler>();
var mediator = builder.BuildMediator();
```

Helpers de teste:
- `FakeEventBus` — eventBus em memória para assertions
- `FakeClock` — clock controlável para testes de scheduling
- `TestCommand`, `TestQuery`, `TestEvent` — tipos prontos para testes

## 17. Configuração

```csharp
// appsettings.json
{
  "FlowCore": {
    "EnableDiagnostics": true,
    "MaxRetryAttempts": 5,
    "RegisterDefaultBehaviors": true,
    "DefaultCacheExpiration": "00:10:00"
  }
}

// Bind
builder.Services.Configure<FlowCoreOptions>(configuration.GetSection("FlowCore"));
```

## 18. Convenções de Código

- `CancellationToken` é sempre o último parâmetro (exceto em delegates)
- Interfaces comenzam com `I`
- Handlers públicos, stores internos (implementações in-memory)
- Usar `ValueTask` para métodos síncronos ou quasi-síncronos
- Usar `Task` para métodos verdadeiramente assíncronos
- Marcadores (`ICommand<T>`, `IQuery<T>`, `IEvent`) são interfaces sem membros
- Nomes de arquivo = nomes de tipo

## 19. Debugging

```csharp
// Acessar contexto de execução atual
var scope = ExecutionScope.Current;
scope?.Correlation.CorrelationId;
scope?.Diagnostics;
scope?.Metrics;
```

Usar `DiagnosticsEventBus` como decorator automaticamente fornece tracing.
`InMemoryDeadLetterWriter.GetMessages()` para inspecionar dead letters.
