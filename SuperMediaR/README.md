
# SuperMediaR

**SuperMediaR** é um Mediator leve, extensível e moderno para .NET 8+, com suporte a padrões como CQRS, Pipeline Behaviors e integração com EF Core. Ideal para aplicações complexas com separação de responsabilidades clara e escalável.

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

## ⚙️ Configuração

### 1. Adicione no seu `Program.cs`:

```csharp
builder.Services.AddSuperMediaR();
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

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se livre para abrir issues ou pull requests.

---

## 📄 Licença

Este projeto é licenciado sob a **MIT License**.
