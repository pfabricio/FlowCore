# Dependency Injection — FlowCore

> How to configure and register services in FlowCore.

---

## 📖 Overview

FlowCore is built on top of .NET's Dependency Injection system. All components are registered and resolved through the DI container.

---

## 🎯 Basic Configuration

```csharp
// Register FlowCore (returns IFlowCoreBuilder for chaining)
builder.Services.AddFlowCore();

// Register DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 📋 Messaging Providers

### RabbitMQ
```csharp
builder.Services.AddFlowCore().AddRabbitMQ(options =>
{
    options.Host = "localhost";
    options.Username = "guest";
    options.Password = "guest";
});
```

### Kafka
```csharp
builder.Services.AddFlowCore().AddKafka(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ConsumerGroup = "my-service";
});
```

---

## 📋 Optional Modules

```csharp
builder.Services.AddFlowCore()
    .AddFlowCoreTransactions()      // TransactionScopeBehavior (EF Core)
    .AddFlowCoreOutbox()             // Outbox Worker (BackgroundService)
    .AddFlowCoreDiagnostics()        // System.Diagnostics Activity + Metrics
    .AddFlowCoreSagaListener()       // Saga event listener
    .AddFlowCoreScheduler();         // Scheduled Messages Worker
```

---

## 📋 Registering Services

### Handlers

```csharp
// Handlers are registered automatically by assembly scanning into IHandlerRegistry
builder.Services.AddFlowCore();
```

### Behaviors

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

### Validators

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();
```

### Cache

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
```

---

## ⚙️ Global Configuration (FlowCoreOptions)

```csharp
builder.Services.Configure<FlowCoreOptions>(options =>
{
    options.MaxRetryAttempts = 5;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(15);
});
```

---

## 🧩 Module System

Custom modules implement `IFlowCoreModule` and register via `IFlowCoreBuilder`:

```csharp
builder.Services.AddFlowCore().AddModule<MyModule>();
```

RabbitMQ and Kafka providers are registered as modules:

```csharp
builder.Services.AddFlowCore().AddRabbitMQ(options => { ... });
builder.Services.AddFlowCore().AddKafka(options => { ... });
```

---

## 🏗️ ExecutionScope

`ExecutionScope` does not require DI registration — accessed via `AsyncLocal`:

```csharp
var scope = ExecutionScope.Current;
scope?.Diagnostics.Write("step", "processing");
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
2. **Respect scopes** - DbContext should be Scoped, EventBus can be Singleton
3. **Avoid circular dependencies** - refactor your design if necessary
4. **Use interfaces** - for easier testing and decoupling
5. **Test your container** - ensure all services are registered
