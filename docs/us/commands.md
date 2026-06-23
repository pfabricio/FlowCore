# Commands — FlowCore

> How to create and use commands in FlowCore.

---

## 📖 Overview

Commands represent actions that change the system state. In FlowCore, commands implement the `ICommand<TResult>` or `ICommand<Unit>` interface.

---

## 🎯 Creating a Command

### Command with Return

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;
```

### Command without Return

```csharp
public record DeleteUserCommand(Guid Id) : ICommand<Unit>;
```

---

## 🔧 Creating a Handler

### Handler with Return

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
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Name = request.Name, 
            Email = request.Email 
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        
        return user.Id;
    }
}
```

### Handler without Return

```csharp
public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Unit>
{
    private readonly MyDbContext _context;

    public DeleteUserCommandHandler(MyDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(request.Id);
        if (user == null)
            throw new NotFoundException("User not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
```

---

## 🚀 Usage

### Dependency Injection

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
        var command = new CreateUserCommand(name, email);
        return await _mediator.SendAsync(command);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var command = new DeleteUserCommand(id);
        await _mediator.SendAsync(command);
    }
}
```

---

## 🔐 Validation

### Using FluentValidation

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

---

## 📝 Best Practices

1. **Use records** for commands - they are immutable and have value equality
2. **Keep commands small** - a command should represent a single action
3. **Use descriptive names** - verbs in infinitive (CreateUser, DeleteOrder)
4. **Always validate** - use FluentValidation to ensure data integrity
5. **Return useful data** - IDs, created entities or success status