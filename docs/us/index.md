# FlowCore — Documentation

> .NET 8+ framework for CQRS, Event-Driven and Microservices. Extensible Mediator with Pipeline Behaviors, multi-provider EventBus, Outbox, Inbox, Saga, Scheduling, Retry, DLQ and OpenTelemetry.

---

## 📚 Getting Started

| Document | Description |
|----------|-------------|
| [Overview](index.md) | What is FlowCore, philosophy and features |
| [Getting Started](getting-started.md) | Installation, setup and first command |
| [Configuration](configuration.md) | DI, builder, providers, global options |

## 📖 API Usage

| Document | Description |
|----------|-------------|
| [Commands](commands.md) | ICommand, ICommandHandler, SendAsync |
| [Queries](queries.md) | IQuery, IQueryHandler, QueryAsync |
| [Events](events.md) | IEvent, IEventHandler, PublishAsync, IEventBus |

## 🔧 Extensibility

| Document | Description |
|----------|-------------|
| [Pipeline](pipeline.md) | Behaviors, phases, ordering, examples |
| [Cache](cache.md) | ICacheableQuery, ICacheProvider, invalidation |
| [Validation](validation.md) | FluentValidation, validation behaviors |
| [Authorization](authorization.md) | Roles, custom rules, authorization |
| [Logging](logging.md) | Logging behavior, tracing, diagnostics |

## 🔌 Integration

| Document | Description |
|----------|-------------|
| [Transactions](transactions.md) | EF Core transactions |
| [Dependency Injection](dependency-injection.md) | Service registration, scopes, lifetimes |

## 🎯 Advanced

| Document | Description |
|----------|-------------|
| [Testing](testing.md) | Unit testing, mocking, patterns |
| [Advanced](advanced.md) | EventBus, Retry, Outbox, Inbox, Saga, Scheduling, Metrics |

---

## ⚡ Quick Start

```csharp
// 1. Configure
builder.Services.AddFlowCore();

// 2. Messaging provider (optional)
builder.Services.AddFlowCore().AddRabbitMQ(options =>
{
    options.Host = "localhost";
});

// 3. Optional modules
builder.Services.AddFlowCore()
    .AddFlowCoreOutbox()
    .AddFlowCoreSagaListener()
    .AddFlowCoreScheduler()
    .AddFlowCoreDiagnostics();

// 4. Create Command
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// 5. Create Handler
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

// 6. Use
var userId = await mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));
```

---

**Version**: 2.0.0 | **License**: MIT
