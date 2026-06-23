
# FlowCore

![NuGet Version](https://img.shields.io/nuget/v/FlowCore)
![NuGet Downloads](https://img.shields.io/nuget/dt/FlowCore)

**FlowCore** é um Mediator leve, extensível e moderno para .NET 8+, com suporte a padrões como CQRS, Pipeline Behaviors e integração com EF Core. Ideal para aplicações complexas com separação de responsabilidades clara e escalável.

## ✨ Recursos

- Suporte a **Commands**, **Queries** e **Events**
- **Pipeline Behaviors** com suporte a:
  - ✔️ Validação (via FluentValidation)
  - ✔️ Logging
  - ✔️ Autorização (com roles e regras customizadas)
  - ✔️ Caching para queries
  - ✔️ Transações com EF Core
  - ✔️ Dispatcher de eventos após execução
- Suporte a múltiplos **Handlers**
- Pronto para ser injetado com **Dependency Injection**
- Separação entre modelos de leitura e escrita (CQRS)

---

## 📦 Instalação

```bash
dotnet add package FlowCore --version 1.1.1
```

---

## ⚙️ Configuração

### 1. Adicione no seu `Program.cs`:

```csharp
builder.Services.AddFlowCore();
```

### 2. Configure o `DbContext`, `FluentValidation` e `ILogger` normalmente:

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

builder.Services.AddLogging();
```

---

## 💡 Exemplo de Uso

### 1. Criando um `Command`

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;
```

### 2. Criando o Handler

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

### 3. Chamando o Mediator

```csharp
var userId = await _mediator.SendAsync(new CreateUserCommand("John", "john@email.com"));
```

---

## 🔐 Autorização com Roles e Regras

### Criando uma regra customizada

```csharp
public class MustBeAdminRule : IAuthorizationRule<CreateUserCommand>
{
    private readonly IUserContext _userContext;

    public MustBeAdminRule(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<bool> IsAuthorizedAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_userContext.Roles.Contains("Admin"));
    }
}
```

---

## 🔄 Cache de Queries

Basta implementar a interface `ICacheableQuery` na query que deseja cachear:

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>, ICacheableQuery
{
    public string CacheKey => $"user:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

---

## 📢 Eventos

```csharp
public record UserCreatedEvent(Guid UserId) : IEvent;

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Novo usuário criado: {@event.UserId}");
        return Task.CompletedTask;
    }
}
```

Os eventos são disparados após o `CommandHandler`, se definidos no `EventDispatcherBehavior`.

---

## 🧪 Testes

Todos os handlers podem ser testados isoladamente. Basta mockar dependências e chamar diretamente o método `Handle`.

---

## 📚 Documentação

A documentação completa está disponível em `docs/`:

### Português (Brasil)
- [Visão Geral](docs/pt-br/index.md)
- [Getting Started](docs/pt-br/getting-started.md)
- [Commands](docs/pt-br/commands.md)
- [Queries](docs/pt-br/queries.md)
- [Events](docs/pt-br/events.md)
- [Pipeline](docs/pt-br/pipeline.md)
- [Cache](docs/pt-br/cache.md)
- [Validation](docs/pt-br/validation.md)
- [Authorization](docs/pt-br/authorization.md)
- [Logging](docs/pt-br/logging.md)
- [Transactions](docs/pt-br/transactions.md)
- [Dependency Injection](docs/pt-br/dependency-injection.md)
- [Testing](docs/pt-br/testing.md)
- [Advanced](docs/pt-br/advanced.md)

### English (US)
- [Overview](docs/us/index.md)
- [Getting Started](docs/us/getting-started.md)
- [Commands](docs/us/commands.md)
- [Queries](docs/us/queries.md)
- [Events](docs/us/events.md)
- [Pipeline](docs/us/pipeline.md)
- [Cache](docs/us/cache.md)
- [Validation](docs/us/validation.md)
- [Authorization](docs/us/authorization.md)
- [Logging](docs/us/logging.md)
- [Transactions](docs/us/transactions.md)
- [Dependency Injection](docs/us/dependency-injection.md)
- [Testing](docs/us/testing.md)
- [Advanced](docs/us/advanced.md)

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se livre para abrir issues ou pull requests.

---

## 📄 Licença

Este projeto é licenciado sob a **MIT License**.
