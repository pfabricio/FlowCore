# FlowCore — Documentação

> Framework .NET 8+ para CQRS, Event-Driven e Microsserviços. Mediator extensível com Pipeline Behaviors, EventBus multi-provider, Outbox, Inbox, Saga, Scheduling, Retry, DLQ, OpenTelemetry, Execution Scope, Handler Discovery, Module System e Source Generator.

---

## 📚 Guia de Início

| Documento | Descrição |
|-----------|-----------|
| [Visão Geral](index.md) | O que é FlowCore, filosofia e features |
| [Getting Started](getting-started.md) | Instalação, setup e primeiro comando |
| [Configuração](configuration.md) | DI, builder, providers, opções globais |

## 📖 Uso da API

| Documento | Descrição |
|-----------|-----------|
| [Commands](commands.md) | ICommand, ICommandHandler, SendAsync |
| [Queries](queries.md) | IQuery, IQueryHandler, QueryAsync |
| [Events](events.md) | IEvent, IEventHandler, PublishAsync, IEventBus |

## 🔧 Extensibilidade

| Documento | Descrição |
|-----------|-----------|
| [Pipeline](pipeline.md) | Behaviors, fases, ordenação, exemplos |
| [Cache](cache.md) | ICacheableQuery, ICacheProvider, invalidação |
| [Validation](validation.md) | FluentValidation, behaviors de validação |
| [Authorization](authorization.md) | Roles, regras customizadas, autorização |
| [Logging](logging.md) | Logging behavior, tracing, diagnósticos |

## 🔌 Integração

| Documento | Descrição |
|-----------|-----------|
| [Transactions](transactions.md) | Transações com EF Core |
| [Dependency Injection](dependency-injection.md) | Registro de serviços, escopos, lifetimes |

## 🎯 Avançado

| Documento | Descrição |
|-----------|-----------|
| [Testing](testing.md) | Testes unitários, mocking, padrões |
| [Advanced](advanced.md) | EventBus, Retry, Outbox, Inbox, Saga, Scheduling, Metrics |

---

## ⚡ Quick Start

```csharp
// 1. Configurar
builder.Services.AddFlowCore();

// 2. Provider de mensageria (opcional)
builder.Services.AddFlowCore().AddRabbitMQ(options =>
{
    options.Host = "localhost";
});

// 3. Módulos opcionais
builder.Services.AddFlowCore()
    .AddFlowCoreOutbox()
    .AddFlowCoreSagaListener()
    .AddFlowCoreScheduler()
    .AddFlowCoreDiagnostics();

// 4. Criar Command
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// 5. Criar Handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly MyDbContext _context;
    public CreateUserCommandHandler(MyDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Id = Guid.NewGuid(), Name = request.Name, Email = request.Email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}

// 6. Usar
var userId = await mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));
```

---

**Versão**: 2.1.0 | **Licença**: MIT
