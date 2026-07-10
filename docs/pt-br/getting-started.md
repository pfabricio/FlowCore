# Getting Started — FlowCore

> Instalação, configuração e primeiros passos com o FlowCore.

---

## 📦 Instalação

### Núcleo
```bash
dotnet add package FlowCore --version 2.2.3
```

### Providers de Mensageria (opcional)
```bash
dotnet add package FlowCore.RabbitMQ --version 2.2.3
dotnet add package FlowCore.Kafka --version 2.2.3
```

### Testing (opcional)
```bash
dotnet add package FlowCore.Testing --version 2.2.3
```

---

## ⚙️ Configuração Básica

### 1. Adicione no seu `Program.cs`:

```csharp
builder.Services.AddFlowCore();
```

### 2. Configure o `DbContext` (opcional):

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 3. Configure `FluentValidation` (opcional):

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
```

### 4. Configure providers (opcional):

```csharp
// RabbitMQ
builder.Services.AddFlowCore().AddRabbitMQ(options =>
{
    options.Host = "localhost";
    options.Username = "guest";
    options.Password = "guest";
});

// Kafka
builder.Services.AddFlowCore().AddKafka(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ConsumerGroup = "my-service";
});
```

### 5. Opções globais (opcional):

```csharp
builder.Services.Configure<FlowCoreOptions>(options =>
{
    options.MaxRetryAttempts = 5;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(15);
});
```

### 6. Módulos opcionais:

```csharp
builder.Services.AddFlowCore()
    .AddFlowCoreTransactions()      // Transações EF Core
    .AddFlowCoreOutbox()             // Outbox Worker
    .AddFlowCoreDiagnostics()        // Tracing + Metrics
    .AddFlowCoreSagaListener()       // Saga event listener
    .AddFlowCoreScheduler();         // Scheduled Messages Worker
    .AddHealthCheck<MyHealthCheck>() // Health Check customizado
    .AddHostedWorker<MyWorker>();    // Worker customizado
```

---

## 🎯 Estrutura Básica

### Commands

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;
```

### Command Handlers

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly MyDbContext _context;

    public CreateUserCommandHandler(MyDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Id = Guid.NewGuid(), Name = request.Name, Email = request.Email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
```

### Queries

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;
```

### Query Handlers

```csharp
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly MyDbContext _context;

    public GetUserByIdQueryHandler(MyDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(request.Id);
        return user == null ? throw new NotFoundException("User not found") : MapToDto(user);
    }
}
```

---

## 🚀 Uso

### Injeção de Dependência

```csharp
public class UserService
{
    private readonly IFlowMediator _mediator;

    public UserService(IFlowMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Guid> CreateUserAsync(string name, string email)
    {
        return await _mediator.SendAsync(new CreateUserCommand(name, email));
    }

    public async Task<UserDto> GetUserAsync(Guid id)
    {
        return await _mediator.QueryAsync(new GetUserByIdQuery(id));
    }
}
```

---

## 📝 Próximos Passos

- [Commands](commands.md) - Saiba mais sobre comandos
- [Queries](queries.md) - Saiba mais sobre consultas
- [Events](events.md) - Eventos e EventBus
- [Pipeline](pipeline.md) - Entenda os behaviors
- [Advanced](advanced.md) - Module Manifest, Health Checks, Resilience, Workers, Saga, Scheduling e mais
