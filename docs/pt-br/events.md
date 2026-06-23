# Events — FlowCore

> Como criar e utilizar eventos no FlowCore.

---

## 📖 Visão Geral

Eventos representam notificações sobre algo que aconteceu no sistema. No FlowCore, eventos implementam a interface `IEvent`.

---

## 🎯 Criando um Event

### Event Simples

```csharp
public record UserCreatedEvent(Guid UserId, string UserName) : IEvent;
```

### Event com Dados

```csharp
public record OrderPlacedEvent(Guid OrderId, Guid UserId, decimal TotalAmount) : IEvent;
```

---

## 🔧 Criando um Handler

### Handler Simples

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

### Handler com Lógica de Negócio

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

## 🚀 Uso

### Disparando Eventos

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

        // Disparar evento após persistir
        await _mediator.PublishAsync(new UserCreatedEvent(user.Id, user.Name), cancellationToken);

        return user.Id;
    }
}
```

### Múltiplos Handlers

```csharp
// Handler para envio de email
public class SendWelcomeEmailHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    public SendWelcomeEmailHandler(IEmailService emailService) => _emailService = emailService;

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(@event.UserId, @event.UserName, cancellationToken);
    }
}

// Handler para auditoria
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

## 📝 Melhores Práticas

1. **Use nomes no passado** - UserCreated, OrderPlaced, PaymentProcessed
2. **Mantenha eventos imutáveis** - não modifique eventos após criação
3. **Inclua dados relevantes** - informações necessárias para handlers
4. **Use múltiplos handlers** - para diferentes ações reativas
5. **Trate erros graciosamente** - handlers não devem falhar o command principal