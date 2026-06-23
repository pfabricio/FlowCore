# Dependency Injection — FlowCore

> How to configure and register services in FlowCore.

---

## 📖 Overview

FlowCore is built on top of .NET's Dependency Injection system. All components are registered and resolved through the DI container.

---

## 🎯 Basic Configuration

### In Program.cs

```csharp
// Register FlowCore
builder.Services.AddFlowCore();

// Register DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 📋 Registering Services

### Handlers

```csharp
// Handlers are registered automatically by assembly scanning
builder.Services.AddFlowCore();
```

### Behaviors

```csharp
// Register behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

### Validators

```csharp
// Register validators via FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();
```

### Cache

```csharp
// In-memory cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
```

---

## 🔧 Scopes and Lifetimes

### Singleton

```csharp
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
```

### Scoped

```csharp
builder.Services.AddScoped<IMyService, MyService>();
```

### Transient

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

---

## 🚀 Usage

### Dependency Injection

```csharp
public class UserService
{
    private readonly IFlowMediator _mediator;
    private readonly MyDbContext _context;

    public UserService(IFlowMediator mediator, MyDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    public async Task<Guid> CreateUserAsync(string name, string email)
    {
        return await _mediator.SendAsync(new CreateUserCommand(name, email));
    }
}
```

### Injection in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IFlowMediator _mediator;

    public UsersController(IFlowMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var userId = await _mediator.SendAsync(command);
        return Ok(new { UserId = userId });
    }
}
```

---

## 📝 Best Practices

1. **Use automatic registration** - when possible, let FlowCore scan assemblies
2. **Respect scopes** - DbContext should be Scoped, Mediator can be Singleton
3. **Avoid circular dependencies** - refactor your design if necessary
4. **Use interfaces** - for easier testing and decoupling
5. **Test your container** - ensure all services are registered