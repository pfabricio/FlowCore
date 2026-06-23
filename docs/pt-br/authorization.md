# Authorization — FlowCore

> Como implementar autorização com roles e regras customizadas no FlowCore.

---

## 📖 Visão Geral

O FlowCore suporta autorização de commands e queries através de Pipeline Behaviors. Isso permite controlar quem pode executar cada operação no sistema.

---

## 🎯 Criando uma Regra de Autorização

### Regra com Role

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

### Regra Customizada

```csharp
public class MustBeOwnerRule : IAuthorizationRule<UpdateUserCommand>
{
    private readonly IUserContext _userContext;

    public MustBeOwnerRule(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<bool> IsAuthorizedAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Usuário pode editar apenas seu próprio perfil
        return Task.FromResult(_userContext.UserId == request.UserId);
    }
}
```

---

## 🔧 Configurando o Authorization Behavior

### No Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddFlowCoreAuthorization();

// Registrar regras de autorização
builder.Services.AddTransient<IAuthorizationRule<CreateUserCommand>, MustBeAdminRule>();
builder.Services.AddTransient<IAuthorizationRule<UpdateUserCommand>, MustBeOwnerRule>();
```

### Behavior Manual

```csharp
public class AuthorizationBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly IEnumerable<IAuthorizationRule<TRequest>> _rules;

    public AuthorizationBehavior(IEnumerable<IAuthorizationRule<TRequest>> rules)
    {
        _rules = rules;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        foreach (var rule in _rules)
        {
            var isAuthorized = await rule.IsAuthorizedAsync(request, cancellationToken);
            if (!isAuthorized)
                throw new UnauthorizedAccessException("User is not authorized to perform this action");
        }

        return await next();
    }
}
```

---

## 🚀 Uso

### Command com Autorização

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// O handler será executado apenas se o usuário estiver autorizado
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    // ...
}
```

### Tratando Erros de Autorização

```csharp
public class UserController : ControllerBase
{
    private readonly IFlowMediator _mediator;

    public UserController(IFlowMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        try
        {
            var userId = await _mediator.SendAsync(command);
            return Ok(new { UserId = userId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
```

---

## 🔐 Autorização com Múltiplas Roles

### Regra com Múltiplas Roles

```csharp
public class MustHaveAnyRoleRule : IAuthorizationRule<DeleteUserCommand>
{
    private readonly IUserContext _userContext;

    public MustHaveAnyRoleRule(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<bool> IsAuthorizedAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var allowedRoles = new[] { "Admin", "SuperAdmin" };
        return Task.FromResult(_userContext.Roles.Any(r => allowedRoles.Contains(r)));
    }
}
```

---

## 📝 Melhores Práticas

1. **Use regras granulares** - uma regra por responsabilidade
2. **Mantenha regras simples** - lógica complexa pode ser difícil de testar
3. **Teste suas regras** - garanta que autorização funciona conforme esperado
4. **Use mensagens claras** - para facilitar debug e experiência do usuário
5. **Considere performance** - regras de autorização não devem ser lentas