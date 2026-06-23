# Validation — FlowCore

> Como implementar validação com FluentValidation no FlowCore.

---

## 📖 Visão Geral

O FlowCore suporta validação automática de commands e queries usando FluentValidation. A validação é executada como um Pipeline Behavior antes do handler.

---

## 🎯 Criando um Validator

### Validator para Command

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email address");
    }
}
```

### Validator para Query

```csharp
public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
```

---

## 🔧 Configurando o Validation Behavior

### No Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

// O ValidationBehavior já está registrado automaticamente
```

### Behavior Manual

```csharp
public class ValidationBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## 🚀 Uso

### Command com Validação

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// O handler será executado apenas se a validação passar
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    // ...
}
```

### Tratando Erros de Validação

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
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                Message = "Validation failed",
                Errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
    }
}
```

---

## 🔐 Validação Condicional

### Validator com Condição

```csharp
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email address");

        // Validação condicional
        When(x => x.Status == UserStatus.Active, () =>
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required for active users");
        });
    }
}
```

---

## 📝 Melhores Práticas

1. **Valide sempre** - use FluentValidation para garantir integridade dos dados
2. **Mantenha validators organizados** - um por command ou query
3. **Use mensagens claras** - para facilitar debug e experiência do usuário
4. **Valide condicionalmente** - quando regras dependem de outros campos
5. **Teste seus validators** - garanta que regras funcionam conforme esperado