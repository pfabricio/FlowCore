# Dependency Injection — FlowCore

> Como configurar e registrar serviços no FlowCore.

---

## 📖 Visão Geral

O FlowCore é construído sobre o sistema de Dependency Injection do .NET. Todos os componentes são registrados e resolvidos através do container de DI.

---

## 🎯 Configuração Básica

### No Program.cs

```csharp
// Registrar FlowCore
builder.Services.AddFlowCore();

// Registrar DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 📋 Registrando Serviços

### Handlers

```csharp
// Handlers são registrados automaticamente por assembly scanning
builder.Services.AddFlowCore();
```

### Behaviors

```csharp
// Registrar behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

### Validators

```csharp
// Registrar validators via FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();
```

### Cache

```csharp
// Cache em memória
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
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

### Injeção de Dependência

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

### Injeção em Controllers

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
2. **Respeite escopos** - DbContext deve ser Scoped, Mediator pode ser Singleton
3. **Evite circular dependencies** - refatore seu design se necessário
4. **Use interfaces** - para facilitar testes e desacoplamento
5. **Teste seu container** - garanta que todos os serviços estão registrados