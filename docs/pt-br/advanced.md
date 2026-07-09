# Advanced — FlowCore

> Tópicos avançados: EventBus, Retry, DLQ, Outbox, Inbox, Saga, Scheduled Messages, Observabilidade, Configuration, Handler Discovery, Execution Scope, Provider Bootstrap, Diagnostics Context, Module System e Source Generator.

---

## 📖 Visão Geral

Este guia cobre os recursos avançados do FlowCore para arquiteturas distribuídas e resilientes.

---

## 🔧 EventBus

### IEventBus

`IEventBus` é a abstração central para publicação distribuída de eventos. O provider padrão é `InMemoryEventBus`. Com os pacotes `FlowCore.RabbitMQ` e `FlowCore.Kafka`, é possível publicar eventos em sistemas de mensageria externos.

```csharp
// Publicar evento
await _eventBus.PublishAsync(new OrderPlacedEvent(orderId, userId, total));

// Provider é resolvido por DI baseado na configuração
builder.Services.AddFlowCore().AddRabbitMQ(options => { ... });
```

### DispatcherCache

Handler resolution usa delegates compilados cacheados em um `DispatcherCache` thread-safe (Singleton), eliminando Reflection no hot path.

---

## 🔄 Resiliência

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

```csharp
public class InMemoryDeadLetterWriter : IDeadLetterWriter
{
    public Task WriteAsync(DeadLetterContext context, CancellationToken ct)
    {
        // persistir mensagem para análise posterior
    }
}
```

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

A verificação acontece automaticamente no `ConsumerWorker.ProcessEnvelopeAsync`.

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

// Registrar
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
// Agendar para daqui 2 horas
await _scheduler.ScheduleAfterAsync(new OrderExpiredEvent(orderId), TimeSpan.FromHours(2));

// Agendar para data específica
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

A validação é automática via `IValidateOptions<FlowCoreOptions>`:
- `MaxRetryAttempts` deve ser >= 0
- `DefaultCacheExpiration` não pode ser zero ou negativo

---

## 🔍 Handler Discovery

Centralizado no `IHandlerRegistry` — substitui o assembly scanning disperso por um registro único com validação de duplicados.

```csharp
public interface IHandlerRegistry
{
    IReadOnlyList<HandlerDescriptor> GetHandlers(Type handlerType);
    bool IsHandlerRegistered(Type handlerType, Type implementationType);
}
```

Registrado automaticamente como Singleton via `AddFlowCore()`. Durante o bootstrap, `HandlerDiscovery` escaneia os assemblies e registra todos os handlers que implementam `ICommandHandler<>`, `IQueryHandler<>` e `IEventHandler<>`.

### Source Generator (opcional)

O pacote `FlowCore.Generators` (Roslyn Incremental Generator) gera o `HandlerRegistry`, `DiRegistration` e `Dispatcher` em compile-time, eliminando a necessidade de Reflection. O runtime tenta usar o código gerado primeiro, com fallback para o discovery via Reflection.

---

## 🏗️ Execution Scope

`IExecutionScope` é um contexto compartilhado por execução, disponível sem injeção de dependência via `AsyncLocal<ExecutionScope?>.Current`.

```csharp
public interface IExecutionScope : IDisposable
{
    string CorrelationId { get; }
    IExecutionItems Items { get; }
    IDiagnosticsContext Diagnostics { get; }
}
```

Criado automaticamente no início de cada `ExecutePipeline` e descartado ao final. Acessível de qualquer ponto do pipeline: Behaviors, Handlers, EventBus e Providers.

```csharp
// Em qualquer lugar do pipeline (sem DI)
var scope = ExecutionScope.Current;
scope?.Diagnostics.Write("step", "handler-executed");
```

---

## 🔌 Provider Registry

`IProviderRegistry` mantém o registro de todos os `IMessageProvider` disponíveis, permitindo descoberta em runtime.

```csharp
public interface IProviderRegistry
{
    IReadOnlyList<IMessageProvider> GetProviders();
    void Register(IMessageProvider provider);
}
```

`IMessageProvider` define um contrato comum com ciclo de vida:

```csharp
public interface IMessageProvider
{
    string Name { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
```

Implementado por `InMemoryEventBus`, `RabbitMqEventBus` e `KafkaEventBus`.

---

## 📊 Diagnostics Context

`IDiagnosticsContext` coleta entradas de diagnóstico durante a execução de uma operação.

```csharp
public interface IDiagnosticsContext
{
    IReadOnlyList<DiagnosticEntry> Entries { get; }
    void Write(string step, string message, object? metadata = null);
}
```

Disponível via `ExecutionScope.Current.Diagnostics`. O `DiagnosticsEventBus` e `ConsumerWorker.ProcessEnvelopeAsync` já escrevem entradas automaticamente.

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

`IFlowCoreBuilder` é a API fluente para configuração do FlowCore, retornada por `AddFlowCore()`.

```csharp
public interface IFlowCoreBuilder
{
    IServiceCollection Services { get; }
    IFlowCoreBuilder AddModule<T>() where T : IFlowCoreModule, new();
}
```

`IFlowCoreModule` permite criar módulos independentes:

```csharp
public class MyModule : IFlowCoreModule
{
    public void Configure(IFlowCoreBuilder builder)
    {
        builder.Services.AddSingleton<IMyService, MyService>();
    }
}

// Uso
builder.Services.AddFlowCore().AddModule<MyModule>();
```

Providers RabbitMQ e Kafka estendem `IFlowCoreBuilder` em vez de `IServiceCollection`.

---

## 📝 Melhores Práticas

1. **Use Outbox** para garantir entrega de eventos críticos
2. **Configure Retry + DLQ** para resiliência contra falhas transitórias
3. **Use Inbox** em consumers para processamento idempotente
4. **Modele Sagas** para transações distribuídas com compensação
5. **Ative Diagnostics** para observabilidade em produção
6. **Agende mensagens** para lógica diferida sem necessidade de cron jobs externos
