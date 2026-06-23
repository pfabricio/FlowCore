# Validation — FlowCore

> How to implement validation with FluentValidation in FlowCore.

---

## 📖 Overview

FlowCore supports automatic command and query validation using FluentValidation. Validation is executed as a Pipeline Behavior before the handler.

---

## 🎯 Creating a Validator

### Validator for Command

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

### Validator for Query

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

## 🔧 Configuring the Validation Behavior

### In Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

// ValidationBehavior is already registered automatically
```

### Manual Behavior

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

## 🚀 Usage

### Command with Validation

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;

// Handler will only execute if validation passes
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    // ...
}
```

### Handling Validation Errors

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

## 🔐 Conditional Validation

### Validator with Condition

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

        // Conditional validation
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

## 📝 Best Practices

1. **Always validate** - use FluentValidation to ensure data integrity
2. **Keep validators organized** - one per command or query
3. **Use clear messages** - for easier debugging and better user experience
4. **Validate conditionally** - when rules depend on other fields
5. **Test your validators** - ensure rules work as expected