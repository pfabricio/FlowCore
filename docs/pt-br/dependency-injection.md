# Dependency Injection — FlowCore

> Como configurar e registrar serviços no FlowCore.

---

## 📖 Visão Geral

O FlowCore é construído sobre o sistema de Dependency Injection do .NET. Todos os componentes são registrados e resolvidos através do container de DI.

---

## 🎯 Configuração Básica

```csharp
// Registrar FlowCore (retorna IFlowCoreBuilder para encadeamento)
builder.Services.AddFlowCore();

// Registrar DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 📋 Providers de Mensageria

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

## 📋 Módulos Opcionais

```csharp
builder.Services.AddFlowCore()
    .AddFlowCoreTransactions()      // TransactionScopeBehavior (EF Core)
    .AddFlowCoreOutbox()             // Outbox Worker (BackgroundService)
    .AddFlowCoreDiagnostics()        // System.Diagnostics Activity + Metrics
    .AddFlowCoreSagaListener()       // Saga event listener
    .AddFlowCoreScheduler();         // Scheduled Messages Worker
```

---

## 📋 Registrando Serviços

### Handlers

```csharp
// Handlers são registrados automaticamente por assembly scanning no IHandlerRegistry
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

## ⚙️ Configuração Global (FlowCoreOptions)

```csharp
builder.Services.Configure<FlowCoreOptions>(options =>
{
    options.MaxRetryAttempts = 5;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(15);
});
```

---

## 🧩 Module System

Módulos personalizados implementam `IFlowCoreModule` e são registrados via `IFlowCoreBuilder`:

```csharp
builder.Services.AddFlowCore().AddModule<MyModule>();
```

Providers RabbitMQ e Kafka são registrados como módulos:

```csharp
builder.Services.AddFlowCore().AddRabbitMQ(options => { ... });
builder.Services.AddFlowCore().AddKafka(options => { ... });
```

---

## 🏗️ ExecutionScope

`ExecutionScope` não requer registro em DI — é acessado via `AsyncLocal`:

```csharp
var scope = ExecutionScope.Current;
scope?.Diagnostics.Write("step", "processing");
```

---

## 🔧 Escopos e Lifetimes

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

## 🚀 Uso

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

## 📝 Melhores Práticas

1. **Use o registro automático** - quando possível, deixe o FlowCore escanear assemblies
2. **Respeite escopos** - DbContext deve ser Scoped, EventBus pode ser Singleton
3. **Evite circular dependencies** - refatore seu design se necessário
4. **Use interfaces** - para facilitar testes e desacoplamento
5. **Teste seu container** - garanta que todos os serviços estão registrados
