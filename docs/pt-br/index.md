# FlowCore — Documentação

> Mediator leve, extensível e moderno para .NET 8+, com suporte a CQRS, Pipeline Behaviors e integração com EF Core.

---

## 📚 Guia de Início

| Documento | Descrição |
|-----------|-----------|
| [Visão Geral](index.md) | O que é FlowCore, filosofia e features |
| [Getting Started](getting-started.md) | Instalação, setup e primeira query |
| [Configuração](configuration.md) | DI, builder, providers, opções globais |

## 📖 Uso da API

| Documento | Descrição |
|-----------|-----------|
| [Commands](commands.md) | ICommand, ICommandHandler, SendAsync |
| [Queries](queries.md) | IQuery, IQueryHandler, QueryAsync |
| [Events](events.md) | IEvent, IEventHandler, PublishAsync |

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
| [Transactions](transactions.md) | ExecutionScope, commit, rollback com EF Core |
| [Dependency Injection](dependency-injection.md) | Registro de serviços, escopos, lifetimes |

## 🎯 Avançado

| Documento | Descrição |
|-----------|-----------|
| [Testing](testing.md) | Testes unitários, mocking, padrões |
| [Advanced](advanced.md) | Multi-database, ExecutionOptions, Metrics |

---

## ⚡ Quick Start

```csharp
// 1. Configurar
builder.Services.AddFlowCore();

// 2. Criar Command
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// 3. Criar Handler
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

// 4. Usar
var userId = await mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));
```

---

**Versão**: 1.1.1 | **Licença**: MIT