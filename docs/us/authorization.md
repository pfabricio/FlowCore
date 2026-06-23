# Authorization — FlowCore

> How to implement authorization with roles and custom rules in FlowCore.

---

## 📖 Overview

FlowCore supports command and query authorization through Pipeline Behaviors. This allows controlling who can execute each operation in the system.

---

## 🎯 Creating an Authorization Rule

### Role-based Rule

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

### Custom Rule

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
        // User can only edit their own profile
        return Task.FromResult(_userContext.UserId == request.UserId);
    }
}
```

---

## 🔧 Configuring the Authorization Behavior

### In Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddFlowCoreAuthorization();

// Register authorization rules
builder.Services.AddTransient<IAuthorizationRule<CreateUserCommand>, MustBeAdminRule>();
builder.Services.AddTransient<IAuthorizationRule<UpdateUserCommand>, MustBeOwnerRule>();
```

### Manual Behavior

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

## 🚀 Usage

### Command with Authorization

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// Handler will only execute if user is authorized
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    // ...
}
```

### Handling Authorization Errors

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

## 🔐 Authorization with Multiple Roles

### Rule with Multiple Roles

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

## 📝 Best Practices

1. **Use granular rules** - one rule per responsibility
2. **Keep rules simple** - complex logic can be hard to test
3. **Test your rules** - ensure authorization works as expected
4. **Use clear messages** - for easier debugging and better user experience
5. **Consider performance** - authorization rules should not be slow