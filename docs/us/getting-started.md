# Getting Started — FlowCore

> Installation, configuration and first steps with FlowCore.

---

## 📦 Installation

```bash
dotnet add package FlowCore --version 1.1.1
```

---

## ⚙️ Basic Configuration

### 1. Add to your `Program.cs`:

```csharp
builder.Services.AddFlowCore();
```

### 2. Configure `DbContext` (optional):

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 3. Configure `FluentValidation` (optional):

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
```

### 4. Configure `Logging` (optional):

```csharp
builder.Services.AddLogging();
```

---

## 🎯 Basic Structure

### Commands

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Guid>;
```

### Command Handlers

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

### Queries

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;
```

### Query Handlers

```csharp
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly MyDbContext _context;

    public GetUserByIdQueryHandler(MyDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(request.Id);
        return user == null ? throw new NotFoundException("User not found") : MapToDto(user);
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
        return await _mediator.SendAsync(new CreateUserCommand(name, email));
    }

    public async Task<UserDto> GetUserAsync(Guid id)
    {
        return await _mediator.QueryAsync(new GetUserByIdQuery(id));
    }
}
```

---

## 📝 Next Steps

- [Commands](commands.md) - Learn more about commands
- [Queries](queries.md) - Learn more about queries
- [Pipeline](pipeline.md) - Understand behaviors
- [Cache](cache.md) - Configure cache for queries