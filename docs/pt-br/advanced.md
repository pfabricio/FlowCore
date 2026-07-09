# Advanced — FlowCore

> Tópicos avançados: Module Manifest, Hosting & Lifecycle, Hosted Workers, Health Checks, Metrics, Resilience, Plugin Model, EventBus, Saga, Scheduling e Testing Infrastructure.

---

## 📖 Visão Geral

Este guia cobre todos os recursos avançados do FlowCore para arquiteturas distribuídas e resilientes.

---

## 🧩 Module Manifest

O Module Manifest representa a identidade oficial de cada módulo do FlowCore.

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

### Criando um módulo com Manifest

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

O Bootstrap valida automaticamente se a versão do FlowCore atende ao `MinimumFlowCoreVersion` de cada módulo.

---

## 🏗️ Hosting & Application Lifecycle

Coordena a inicialização e o encerramento ordenados de todos os componentes.

### BootstrapCoordinator

O Bootstrap segue a ordem:

```
Core → Configuration → Providers → Workers → Application Ready
```

O Shutdown ocorre na ordem inversa:

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

Integrado automaticamente ao .NET Generic Host via `BootstrapHostedService`. Falhas durante startup abortam a aplicação; falhas no shutdown não bloqueiam os demais componentes.

---

## 🔄 Hosted Workers

Workers são componentes que executam processamento contínuo em segundo plano. Cada unidade de trabalho possui seu próprio `ExecutionScope`.

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

### Registro

```csharp
builder.Services.AddFlowCore()
    .AddHostedWorker<MyCustomWorker>();
```

Workers seguem o mesmo ciclo de vida: Create → Start → Execute (loop) → Stop → Dispose. Cada iteração cria e descarta um `ExecutionScope`.

---

## ❤️ Health Checks

Sistema unificado para verificar o estado operacional de componentes.

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

### Criando um Health Check

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

// Registro
builder.Services.AddFlowCore().AddHealthCheck<RabbitMqHealthCheck>();
```

O `IHealthCheckRegistry` agrega todos os checks registrados e expõe o estado geral (pior estado encontrado).

---

## 📊 Metrics Context

Coleta de métricas por `ExecutionScope`, permitindo instrumentação de Pipeline, EventBus, Providers e Workers.

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

### Uso no Pipeline

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

Cada `ExecutionScope` possui seu próprio `MetricsContext`. As métricas são isoladas por execução e nunca compartilhadas.

---

## 🛡️ Resilience

Políticas de resiliência unificadas sob a interface `IResiliencePolicy`.

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

Limita o tempo máximo de execução.

```csharp
var timeout = new TimeoutPolicy(TimeSpan.FromSeconds(5));
await policy.ExecuteAsync(ct => MyOperationAsync(ct), ct);
```

### CircuitBreakerPolicy

Protege contra falhas consecutivas.

```csharp
var circuit = new CircuitBreakerPolicy(
    failureThreshold: 5,
    openDuration: TimeSpan.FromSeconds(30));

// Estados: Closed → Open → HalfOpen → Closed
```

### BulkheadPolicy

Limita o número de operações concorrentes.

```csharp
var bulkhead = new BulkheadPolicy(maxConcurrency: 10);
```

### FallbackPolicy

Executa uma estratégia alternativa em caso de falha.

```csharp
var fallback = new FallbackPolicy(() => Console.WriteLine("Fallback executed"));
```

### RateLimiterPolicy

Controla a taxa de execução.

```csharp
var rateLimiter = new RateLimiterPolicy(maxRequestsPerSecond: 100);
```

### PolicyComposer

Combina múltiplas políticas em uma pipeline.

```csharp
var pipeline = new PolicyComposer(
    new CircuitBreakerPolicy(failureThreshold: 3),
    new TimeoutPolicy(TimeSpan.FromSeconds(10)),
    new RetryPolicy(maxAttempts: 3));

var result = await pipeline.ExecuteAsync(ct => MyOperationAsync(ct), ct);
```

A ordem de execução é da primeira para a última política (outer → inner).

---

## 🔧 EventBus

### IEventBus

`IEventBus` é a abstração central para publicação distribuída de eventos. O provider padrão é `InMemoryEventBus`. Com os pacotes `FlowCore.RabbitMQ` e `FlowCore.Kafka`, é possível publicar eventos em sistemas de mensageria externos.

```csharp
await _eventBus.PublishAsync(new OrderPlacedEvent(orderId, userId, total));

builder.Services.AddFlowCore().AddRabbitMQ(options => { ... });
```

### DispatcherCache

Handler resolution usa delegates compilados cacheados em um `DispatcherCache` thread-safe (Singleton), eliminando Reflection no hot path. O Source Generator pode gerar o dispatcher em compile-time.

---

## 🔄 Resiliência (Retry e DLQ)

### Retry Policy

`IRetryPolicy` define a estratégia de retentativa para mensagens com falha.

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

Mensagens que excedem o limite de tentativas são enviadas para a DLQ via `IDeadLetterWriter`.

---

## 📦 Outbox

O padrão Outbox garante publicação confiável de eventos: a mensagem é salva no banco junto com a transação do handler, e um `OutboxWorker` (BackgroundService) publica para o EventBus em background.

```csharp
builder.Services.AddFlowCore().AddFlowCoreOutbox();
```

Componentes:
- `IOutboxStore` — armazenamento de mensagens (InMemory ou custom)
- `OutboxMessage` — entidade com Id, Tipo, Dados, Status e Timestamp
- `OutboxWorker` — processa mensagens pendentes em loop

---

## 📥 Inbox

O padrão Inbox garante processamento idempotente: mensagens recebidas são verificadas por `MessageId` antes de serem processadas.

```csharp
public interface IInboxStore
{
    Task<bool> IsMessageProcessedAsync(string messageId, CancellationToken ct);
    Task MarkAsProcessedAsync(InboxMessage message, CancellationToken ct);
}
```

---

## 🎭 Saga

Suporte a Saga Orchestration para coordenação de transações distribuídas com compensação.

```csharp
public class OrderSaga : Saga
{
    public override Task DefineStepsAsync()
    {
        AddStep<OrderPlacedEvent>("ReserveInventory", async (evt, ct) =>
        {
            // lógica principal
        }, compensate: async (evt, ct) =>
        {
            // lógica de compensação (ordem reversa)
        });

        AddStep<PaymentProcessedEvent>("ProcessPayment", async (evt, ct) =>
        {
            // próximo passo
        });

        return Task.CompletedTask;
    }
}

builder.Services.AddSaga<OrderSaga>();
builder.Services.AddFlowCoreSagaListener();
```

Componentes:
- `SagaCoordinator` — orquestra steps e compensação
- `ISagaStore` — persiste estado da saga
- `SagaEventListener` — escuta eventos e aciona steps

---

## ⏰ Scheduled Messages

Agende mensagens para publicação futura (absoluta ou relativa).

```csharp
await _scheduler.ScheduleAfterAsync(new OrderExpiredEvent(orderId), TimeSpan.FromHours(2));
await _scheduler.ScheduleAtAsync(new ReminderEvent(userId), DateTimeOffset.UtcNow.AddDays(1));
```

```csharp
builder.Services.AddFlowCore().AddFlowCoreScheduler();
```

Componentes:
- `IMessageScheduler` — interface de agendamento
- `IScheduledMessageStore` — persistência de mensagens agendadas
- `SchedulerWorker` — BackgroundService que publica mensagens na hora certa

---

## 📊 Observabilidade

### DiagnosticsEventBus

Decorator que adiciona tracing e métricas ao EventBus. No-op por padrão (zero overhead); ativado com:

```csharp
builder.Services.AddFlowCore().AddFlowCoreDiagnostics();
```

### Activity & Metrics

- `IActivityFactory` / `SystemDiagnosticsActivityFactory` — cria `Activity` para tracing distribuído
- `IMetricRecorder` / `SystemDiagnosticsMetricRecorder` — registra métricas com `System.Diagnostics.Metrics`
- CorrelationId é propagado no envelope da mensagem

---

## ⚙️ Configuration (FlowCoreOptions)

Opções globais do framework configuráveis via `IConfigureOptions<FlowCoreOptions>` ou `appsettings.json`.

```csharp
builder.Services.Configure<FlowCoreOptions>(options =>
{
    options.MaxRetryAttempts = 5;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(15);
});
```

Validação automática via `IValidateOptions<FlowCoreOptions>`:
- `MaxRetryAttempts` deve ser >= 0
- `DefaultCacheExpiration` não pode ser zero ou negativo

---

## 🔍 Handler Discovery

Centralizado no `IHandlerRegistry` e `HandlerDiscovery`. O discovery é **híbrido**: primeiro tenta carregar o `GeneratedHandlerRegistry` gerado pelo Source Generator em compile-time; se não encontrado, faz fallback para reflection.

```csharp
public interface IHandlerRegistry
{
    HandlerDescriptor? GetHandler(Type requestType);
    IReadOnlyCollection<HandlerDescriptor> GetEventHandlers(Type eventType);
}
```

Registrado automaticamente como Singleton via `AddFlowCore()`.

### Source Generator (híbrido)

O pacote `FlowCore.Generators` (Roslyn Incremental Generator) gera:
- `GeneratedDispatcher.g.cs` — dispatch type-safe sem reflection
- `GeneratedHandlerRegistry.g.cs` — registro de handlers em compile-time
- `GeneratedDiRegistration.g.cs` — registro de DI sem assembly scanning

O runtime tenta usar o código gerado primeiro. Se não existir (projeto sem Source Generator), faz fallback para reflection com anotações `[RequiresDynamicCode]` e `[RequiresUnreferencedCode]`.

---

## 🏗️ Execution Scope

`IExecutionScope` é um contexto compartilhado por execução, disponível sem injeção de dependência via `AsyncLocal<ExecutionScope?>.Current`.

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

Criado automaticamente no início de cada `ExecutePipeline` e descartado ao final. Acessível de qualquer ponto do pipeline:

```csharp
var scope = ExecutionScope.Current;
scope?.Diagnostics.Write("step", "handler-executed");
scope?.Metrics.RecordCounter("requests.total");
```

---

## 🔌 Provider Registry

`IProviderRegistry` mantém o registro de todos os `IMessageProvider` disponíveis. O ciclo de vida (Start/Stop) é gerenciado pelo `BootstrapCoordinator`.

```csharp
public interface IMessageProvider
{
    string Name { get; }
    ValueTask StartAsync(CancellationToken ct);
    ValueTask StopAsync(CancellationToken ct);
}
```

Implementado por `InMemoryEventBus`, `RabbitMqEventBus` e `KafkaEventBus`.

---

## 📊 Diagnostics Context

`IDiagnosticsContext` coleta entradas de diagnóstico durante a execução.

```csharp
public interface IDiagnosticsContext
{
    IReadOnlyList<DiagnosticEntry> Entries { get; }
    void Write(string step, string message, object? metadata = null);
}
```

Disponível via `ExecutionScope.Current.Diagnostics`.

---

## 🧩 Module System

`IFlowCoreBuilder` é a API fluente para configuração do FlowCore, retornada por `AddFlowCore()`.

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

`IFlowCoreModule` permite criar módulos independentes com Manifest obrigatório:

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

Plugins são módulos de terceiros que seguem exatamente os mesmos contratos dos módulos oficiais.

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

### Exemplo de Plugin

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

O Bootstrap valida automaticamente a compatibilidade de versão. Plugins podem registrar Providers, Workers, Behaviors, Health Checks e Metrics — sem qualquer alteração no Core.

---

## 🧪 Testing Infrastructure

O pacote `FlowCore.Testing` fornece infraestrutura para testar aplicações baseadas no FlowCore.

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

// Executar cenário
var result = await mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));

// Verificar eventos publicados
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

## 📝 Melhores Práticas

1. **Use Outbox** para garantir entrega de eventos críticos
2. **Configure Retry + DLQ** para resiliência contra falhas transitórias
3. **Use Inbox** em consumers para processamento idempotente
4. **Modele Sagas** para transações distribuídas com compensação
5. **Ative Diagnostics** para observabilidade em produção
6. **Agende mensagens** para lógica diferida sem necessidade de cron jobs externos
7. **Defina Module Manifests** para todos os módulos e plugins
8. **Registre Health Checks** para monitoramento operacional
9. **Utilize o FlowCore.Testing** para testes sem infraestrutura externa
10. **Prefira Source Generators** para compatibilidade com Native AOT
