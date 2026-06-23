# Commands — FlowCore

> Como criar e utilizar comandos no FlowCore.

---

## 📖 Visão Geral

Comandos representam ações que alteram o estado do sistema. No FlowCore, comandos implementam a interface `ICommand<TResult>` ou `ICommand<Unit>`.

---

## 🎯 Criando um Command

### Command com Retorno

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;
```

### Command sem Retorno

```csharp
public record DeleteUserCommand(Guid Id) : ICommand<Unit>;
```

---

## 🔧 Criando um Handler

### Handler com Retorno

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

### Handler sem Retorno

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

## 🔐 Validação

### Usando FluentValidation

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

## 📝 Melhores Práticas

1. **Use records** para commands - são imutáveis e possuem igualdade por valor
2. **Mantenha commands pequenos** - um command deve representar uma única ação
3. **Use nomes descritivos** - verbos no infinitivo (CreateUser, DeleteOrder)
4. **Valide sempre** - use FluentValidation para garantir integridade dos dados
5. **Retorne dados úteis** - IDs, entidades criadas ou status de sucesso