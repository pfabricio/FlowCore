# FlowCore

![NuGet Version](https://img.shields.io/nuget/v/FlowCore)
![NuGet Downloads](https://img.shields.io/nuget/dt/FlowCore)

**FlowCore** é um Mediator leve, extensível e moderno para .NET 8+, com suporte a padrões como CQRS, Pipeline Behaviors e integração com EF Core. Ideal para aplicações complexas com separação de responsabilidades clara e escalável.

## Recursos

- Suporte a **Commands**, **Queries** e **Events**
- **Pipeline Behaviors** com suporte a:
  - Validação (via FluentValidation)
  - Logging com tempo de execução
  - Caching para queries
  - Transações com EF Core (opcional)
  - Dispatcher de eventos após execução
- Suporte a múltiplos **Handlers** (incluindo multicast para eventos)
- Pronto para ser injetado com **Dependency Injection**
- Separação entre modelos de leitura e escrita (CQRS)
- Commands sem retorno usando `Unit`
- **Auto-registro** via Scrutor para handlers e behaviors

## Instalação

```bash
dotnet add package FlowCore --version 1.1.1
```

Ou última versão:

```bash
dotnet add package FlowCore
```

Para suporte a transações com EF Core:

```bash
dotnet add package FlowCore
# A dependência do EF Core será incluída automaticamente
```

## Configuração

### 1. Configuração básica (sem EF Core)

```csharp
using FlowCore;

// Registra o FlowCore com todos os assemblies
builder.Services.AddFlowCore();

// Ou registre apenas assemblies específicos
builder.Services.AddFlowCore(typeof(Program).Assembly);
```

### 2. Configuração com transações EF Core

```csharp
using FlowCore;

// Configuração básica
builder.Services.AddFlowCore();

// Adiciona suporte a transações
builder.Services.AddFlowCoreTransactions();

// Configuração do EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

### 3. Configurar dependências necessárias

```csharp
// FluentValidation (obrigatório para ValidationBehavior)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Logging
builder.Services.AddLogging();

// Cache (obrigatório implementar para CachingBehavior funcionar)
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
```

## Exemplos de Uso

### Commands

```csharp
// Command com retorno
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly AppDbContext _context;

    public CreateUserCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { Id = Guid.NewGuid(), Name = command.Name, Email = command.Email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user.Id;
    }
}

// Uso
var userId = await _mediator.SendAsync(new CreateUserCommand("João", "joao@email.com"));
```

### Commands sem retorno

```csharp
using FlowCore.Core;

public record DeactivateUserCommand(Guid UserId) : ICommand<Unit>;

public class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeactivateUserCommand command, CancellationToken ct)
    {
        // ... lógica ...
        return Unit.Value;
    }
}

// Uso
await _mediator.SendAsync(new DeactivateUserCommand(userId));
```

### Queries

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly AppDbContext _context;

    public GetUserByIdQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> HandleAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.Id, ct);

        return new UserDto { Id = user.Id, Name = user.Name, Email = user.Email };
    }
}

// Uso
var user = await _mediator.QueryAsync(new GetUserByIdQuery(userId));
```

### Queries com Cache

```csharp
public record GetProductByIdQuery(Guid Id) : IQuery<ProductDto>, ICachableQuery<ProductDto>
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

### Eventos

```csharp
public record OrderCreatedEvent(Guid OrderId) : IEvent;

public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"Pedido criado: {@event.OrderId}");
        return Task.CompletedTask;
    }
}

// Uso
await _mediator.PublishAsync(new OrderCreatedEvent(orderId));
```

### Múltiplos Handlers para um mesmo Evento

O `FlowMediator.PublishAsync()` suporta multicast — você pode registrar vários handlers para o mesmo evento:

```csharp
public record OrderCreatedEvent(Guid OrderId) : IEvent;

// Handler 1: Envia e-mail
public class SendEmailOnOrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"Enviando e-mail para pedido {@event.OrderId}");
        return Task.CompletedTask;
    }
}

// Handler 2: Atualiza estoque
public class UpdateStockOnOrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"Atualizando estoque para pedido {@event.OrderId}");
        return Task.CompletedTask;
    }
}

// Handler 3: Registra em log de auditoria
public class AuditLogOnOrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"Audit: Pedido {@event.OrderId} criado");
        return Task.CompletedTask;
    }
}

// Todos os 3 handlers serão executados quando o evento for publicado
await _mediator.PublishAsync(new OrderCreatedEvent(orderId));
```

### Eventos automáticos via IEventSource

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>, IEventSource
{
    public IEnumerable<IEvent> Events { get; private set; } = Array.Empty<IEvent>();

    public class Handler : ICommandHandler<CreateUserCommand, Guid>
    {
        public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct)
        {
            var userId = Guid.NewGuid();
            // ... salvar no banco ...

            // Define eventos que serão disparados após o handler
            command.Events = new IEvent[] { new UserCreatedEvent(userId) };
            return userId;
        }
    }
}
```

## Interface IFlowMediator

O `IFlowMediator` é o ponto de entrada principal para todas as operações CQRS:

```csharp
public interface IFlowMediator
{
    // Envia um comando que retorna um resultado
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    // Envia um comando sem retorno (void)
    Task SendAsync(ICommand<Unit> command, CancellationToken cancellationToken = default);

    // Executa uma query e retorna o resultado
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    // Publica um evento para todos os handlers registrados
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
```

### Injeção de Dependência

```csharp
// Registrar o mediator
builder.Services.AddScoped<IFlowMediator, FlowMediator>();

// Usar via construtor
public class MyService
{
    private readonly IFlowMediator _mediator;

    public MyService(IFlowMediator mediator)
    {
        _mediator = mediator;
    }
}
```

## Pipeline Behaviors

| Ordem | Behavior | Função |
|-------|----------|--------|
| 1 | `LoggingBehavior` | Log entrada/saída com tempo de execução |
| 2 | `ValidationBehavior` | Valida com FluentValidation — lança `ValidationException` |
| 3 | `CachingBehavior` | Cache para queries que implementam `ICachableQuery` |
| 4 | `TransactionScopeBehavior` | Transação EF Core com commit/rollback automático (opcional) |
| 5 | `EventDispatcherBehavior` | Despacha eventos de `IEventSource` após handler |

### Fluxo de execução

```
SendAsync(command)
  → LoggingBehavior (log entrada)
  → ValidationBehavior (valida)
  → CachingBehavior (skip para commands)
  → TransactionScopeBehavior (begin transaction) [se registrado]
  → EventDispatcherBehavior (executa handler → extrai eventos → dispatch)
  → ICommandHandler.HandleAsync() ← execução real
  → LoggingBehavior (log saída + tempo)
```

### Controle da Ordem dos Behaviors

O pipeline executa behaviors na ordem inversa do registro (último registrado = primeiro a executar). Para controlar a ordem:

```csharp
// Registrar behaviors na ordem desejada (serão executados na ordem inversa)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ThirdBehavior<,>));  // Executa 3º
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondBehavior<,>)); // Executa 2º
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FirstBehavior<,>));  // Executa 1º

// Ou usar Scrutor para registro automático (ordem baseado na ordem de varredura)
services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
    .AsImplementedInterfaces()
    .Scoped());
```

## Implementar ICacheProvider

O FlowCore não fornece implementação padrão. Exemplo com `MemoryCache`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using FlowCore.Core.Interfaces;

public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;

    public MemoryCacheProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration.Value;

        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
```

## Custom Behavior

```csharp
using FlowCore.Core.Interfaces;

public class AuthorizationBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Lógica de autorização antes do handler
        // if (!IsAuthorized(request)) throw new UnauthorizedAccessException();

        return await next();
    }
}

// Registrar no DI (adiciona ao pipeline)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
```

## Tratamento de Erros

### Validação

O `ValidationBehavior` lança `FluentValidation.ValidationException` quando a validação falha:

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");
    }
}

// Tratar erros de validação
try
{
    await _mediator.SendAsync(new CreateUserCommand("", "invalid-email"));
}
catch (ValidationException ex)
{
    // ex.Errors contém todos os erros de validação
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

### Cache

O `CachingBehavior` ignora silenciosamente se:
- `ICacheProvider` não estiver registrado no DI
- A query não implementar `ICachableQuery<TResult>`
- O cache key for nulo ou vazio

```csharp
// Se ICacheProvider não estiver registrado, a query será executada normalmente
// sem cache, sem erros
var user = await _mediator.QueryAsync(new GetUserByIdQuery(userId));
```

### Transações

O `TransactionScopeBehavior` faz rollback automático em caso de exceção:

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { Name = command.Name, Email = command.Email };
        _context.Users.Add(user);

        // Se SaveChangesAsync lançar exceção, a transação será revertida
        await _context.SaveChangesAsync(ct);
        return user.Id;
    }
}
```

### Eventos

O `EventDispatcherBehavior` captura exceções dos handlers de eventos e as propaga:

```csharp
// Se um handler de evento lançar exceção, ela será propagada
// mas os handlers anteriores já terão sido executados
public class FailEventHandler : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        throw new InvalidOperationException("Erro ao processar evento");
    }
}
```

## Pontos de Atenção

- **EF Core opcional**: `TransactionScopeBehavior` só está disponível se você chamar `AddFlowCoreTransactions()`
- **ICacheProvider**: obrigatório implementar — sem isso, `CachingBehavior` ignora silenciosamente
- **Transações**: `TransactionScopeBehavior` usa transações separadas por DbContext. Não suporta two-phase commit para bancos diferentes
- **EventDispatcherBehavior**: usa reflection para despachar eventos
- **Behaviors em ordem inversa**: o pipeline encadeia behaviors em ordem reversa (`Reverse()`), então o último registrado é o primeiro a executar

## Licença

Este projeto é licenciado sob a **MIT License**.
