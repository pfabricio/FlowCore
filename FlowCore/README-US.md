# 📦 FlowCore

**FlowCore** is a lightweight, extensible, and modern Mediator for .NET 8+, supporting patterns such as CQRS, Pipeline Behaviors, and EF Core integration.

## 🎯 Main Features
- Support for **Commands**, **Queries**, and **Events**
- **Pipeline Behaviors** with:
  - Validation (via FluentValidation)
  - Logging with execution time
  - Caching for queries
  - Transactions with EF Core (optional)
  - Event dispatcher after execution
- Support for multiple **Handlers** (multicast for events)
- Ready for **Dependency Injection**
- CQRS separation between read and write
- **Auto-registration** via Scrutor

## 📥 Installation
```bash
dotnet add package FlowCore --version 1.1.1
```

## ⚙️ Pipeline Behaviors (execution order)
| Order | Behavior | Function |
|-------|----------|----------|
| 1 | `LoggingBehavior` | Log input/output with timing |
| 2 | `ValidationBehavior` | Validates with FluentValidation |
| 3 | `CachingBehavior` | Cache for queries |
| 4 | `TransactionScopeBehavior` | EF Core transaction (optional) |
| 5 | `EventDispatcherBehavior` | Dispatches events after handler |

## 🔧 Main Interface
```csharp
public interface IFlowMediator
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task SendAsync(ICommand<Unit> command, CancellationToken ct = default);
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
    Task PublishAsync(IEvent @event, CancellationToken ct = default);
}
```

## 📚 Usage Examples

### 1️⃣ Defining a Command
```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public Task<Guid> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // Business logic
        return Task.FromResult(Guid.NewGuid());
    }
}
```

### 2️⃣ Defining a Query
```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;

public class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        // Search logic
        return Task.FromResult(new UserDto { Id = request.Id, Name = "John" });
    }
}
```

### 3️⃣ Defining an Event
```csharp
public record UserCreatedEvent(Guid UserId) : IEvent;

public class UserCreatedHandler : IEventHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent notification, CancellationToken ct)
    {
        // Send email, log, etc.
        return Task.CompletedTask;
    }
}
```

### 4️⃣ Registering in DI
```csharp
builder.Services.AddFlowCore(cfg =>
{
    cfg.RegisterHandlersFromAssemblyOf<Program>();
});
```

### 5️⃣ Using the Mediator
```csharp
public class UserService(IFlowMediator mediator)
{
    public async Task<Guid> CreateUser(string name, string email)
    {
        return await mediator.SendAsync(new CreateUserCommand(name, email));
    }

    public async Task<UserDto> GetUser(Guid id)
    {
        return await mediator.QueryAsync(new GetUserByIdQuery(id));
    }

    public async Task NotifyCreation(Guid userId)
    {
        await mediator.PublishAsync(new UserCreatedEvent(userId));
    }
}
```

## 🔐 Validation with FluentValidation
```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress();
    }
}
```

## 💾 Caching Configuration
```csharp
builder.Services.AddFlowCore(cfg =>
{
    cfg.RegisterHandlersFromAssemblyOf<Program>();
    cfg.UseCaching(new CachingOptions
    {
        DefaultExpiration = TimeSpan.FromMinutes(5),
        CacheKeyGenerator = new CustomCacheKeyGenerator()
    });
});
```

## 🔄 Transactions with EF Core
```csharp
builder.Services.AddFlowCore(cfg =>
{
    cfg.RegisterHandlersFromAssemblyOf<Program>();
    cfg.UseTransactionScope(new TransactionOptions
    {
        IsolationLevel = IsolationLevel.ReadCommitted
    });
});
```

## 🎨 Custom Behaviors
```csharp
public class MyCustomBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Before handler
        var response = await next();
        // After handler
        return response;
    }
}

// Register
builder.Services.AddFlowCore(cfg =>
{
    cfg.RegisterHandlersFromAssemblyOf<Program>();
    cfg.AddBehavior(typeof(MyCustomBehavior<,>));
});
```

## 📄 License
This project is licensed under the MIT License.
