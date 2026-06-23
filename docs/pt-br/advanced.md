# Advanced — FlowCore

> Tópicos avançados do FlowCore: métricas, multi-database e opções avançadas.

---

## 📖 Visão Geral

Este guia cobre tópicos avançados do FlowCore para cenários mais complexos de uso.

---

## 🔧 ExecutionOptions

### Configurando ExecutionOptions

```csharp
builder.Services.AddFlowCore(options =>
{
    options.EnableValidation = true;
    options.EnableLogging = true;
    options.EnableCaching = true;
    options.EnableTransactions = true;
    options.DefaultCacheExpiration = TimeSpan.FromMinutes(10);
});
```

### ExecutionOptions customizado

```csharp
public class CustomFlowCoreOptions
{
    public bool EnableMetrics { get; set; } = true;
    public string MetricPrefix { get; set; } = "flowcore";
    public TimeSpan SlowRequestThreshold { get; set; } = TimeSpan.FromSeconds(1);
}
```

---

## 📊 Métricas

### Configurando Métricas

```csharp
builder.Services.AddFlowCore();

// Adicionar behavior de métricas
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));
```

### Metrics Behavior

```csharp
public class MetricsBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly ILogger<MetricsBehavior<TRequest, TResult>> _logger;

    public MetricsBehavior(ILogger<MetricsBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            _logger.LogInformation("METRIC:{RequestName}:duration:{Duration}ms:status:success",
                requestName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError("METRIC:{RequestName}:duration:{Duration}ms:status:error:{Error}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
```

---

## 🗄️ Multi-Database

### Configuração para Múltiplos Databases

```csharp
builder.Services.AddDbContext<ReadDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReadConnection")));

builder.Services.AddDbContext<WriteDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WriteConnection")));
```

### Handlers para Diferentes Contextos

```csharp
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly ReadDbContext _readContext;

    public GetUserByIdQueryHandler(ReadDbContext readContext)
    {
        _readContext = readContext;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _readContext.Users
            .Where(u => u.Id == request.Id)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User not found");
    }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly WriteDbContext _writeContext;

    public CreateUserCommandHandler(WriteDbContext writeContext)
    {
        _writeContext = writeContext;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Id = Guid.NewGuid(), Name = request.Name };
        _writeContext.Users.Add(user);
        await _writeContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
```

---

## 🔐 Event Dispatcher Behavior

### Behavior para Disparar Eventos

```csharp
public class EventDispatcherBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly IFlowMediator _mediator;
    private readonly ILogger<EventDispatcherBehavior<TRequest, TResult>> _logger;

    public EventDispatcherBehavior(IFlowMediator mediator, ILogger<EventDispatcherBehavior<TRequest, TResult>> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var result = await next();

        if (request is ICommand<TResult> command && command.Events?.Any() == true)
        {
            foreach (var @event in command.Events)
            {
                _logger.LogInformation("Publishing event {EventType}", @event.GetType().Name);
                await _mediator.PublishAsync(@event, cancellationToken);
            }
        }

        return result;
    }
}
```

---

## 📝 Melhores Práticas

1. **Use ExecutionOptions** - para configurar comportamentos globais
2. **Monitore métricas** - para identificar gargalos e problemas
3. **Considere multi-database** - para separar leitura e escrita
4. **Use Event Dispatcher** - para automatizar publicação de eventos
5. **Teste cenários avançados** - garanta que configurações complexas funcionam