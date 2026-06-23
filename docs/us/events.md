# Events — FlowCore

> How to create and use events in FlowCore.

---

## 📖 Overview

Events represent notifications about something that happened in the system. In FlowCore, events implement the `IEvent` interface.

---

## 🎯 Creating an Event

### Simple Event

```csharp
public record UserCreatedEvent(Guid UserId, string UserName) : IEvent;
```

### Event with Data

```csharp
public record OrderPlacedEvent(Guid OrderId, Guid UserId, decimal TotalAmount) : IEvent;
```

---

## 🔧 Creating a Handler

### Simple Handler

```csharp
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} - {UserName}", @event.UserId, @event.UserName);
        return Task.CompletedTask;
    }
}
```

### Handler with Business Logic

```csharp
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(IEmailService emailService, ILogger<UserCreatedEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing UserCreatedEvent for user {UserId}", @event.UserId);

        await _emailService.SendWelcomeEmailAsync(@event.UserId, @event.UserName, cancellationToken);

        _logger.LogInformation("Welcome email sent to user {UserId}", @event.UserId);
    }
}
```

---

## 🚀 Usage

### Publishing Events

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly MyDbContext _context;
    private readonly IFlowMediator _mediator;

    public CreateUserCommandHandler(MyDbContext context, IFlowMediator mediator)
    {
        _context = context;
        _mediator = mediator;
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

        // Publish event after persisting
        await _mediator.PublishAsync(new UserCreatedEvent(user.Id, user.Name), cancellationToken);

        return user.Id;
    }
}
```

### Multiple Handlers

```csharp
// Handler for sending email
public class SendWelcomeEmailHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    public SendWelcomeEmailHandler(IEmailService emailService) => _emailService = emailService;

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(@event.UserId, @event.UserName, cancellationToken);
    }
}

// Handler for auditing
public class AuditUserCreatedHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IAuditService _auditService;
    public AuditUserCreatedHandler(IAuditService auditService) => _auditService = auditService;

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        await _auditService.LogAsync("UserCreated", @event.UserId, cancellationToken);
    }
}
```

---

## 📝 Best Practices

1. **Use past tense names** - UserCreated, OrderPlaced, PaymentProcessed
2. **Keep events immutable** - do not modify events after creation
3. **Include relevant data** - information needed by handlers
4. **Use multiple handlers** - for different reactive actions
5. **Handle errors gracefully** - handlers should not fail the main command