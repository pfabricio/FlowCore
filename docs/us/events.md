# Events — FlowCore

> How to create and use events and the EventBus in FlowCore.

---

## 📖 Overview

Events represent notifications about something that happened in the system. In FlowCore, events implement the `IEvent` interface.

There are two ways to publish events:
- **InMemory** via `IFlowMediator.PublishAsync()` — events are processed in the same process
- **Distributed** via `IEventBus` — events can be published to external providers (RabbitMQ, Kafka) with Outbox, Retry and Inbox support

---

## 🎯 Creating an Event

```csharp
public record UserCreatedEvent(Guid UserId, string UserName) : IEvent;
public record OrderPlacedEvent(Guid OrderId, Guid UserId, decimal TotalAmount) : IEvent;
```

---

## 🔧 Creating a Handler

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

---

## 🚀 Usage with IFlowMediator (InMemory)

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
        var user = new User { Id = Guid.NewGuid(), Name = request.Name, Email = request.Email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        await _mediator.PublishAsync(new UserCreatedEvent(user.Id, user.Name), cancellationToken);
        return user.Id;
    }
}
```

---

## 🚀 Usage with IEventBus (Distributed)

```csharp
public class OrderService
{
    private readonly IEventBus _eventBus;

    public OrderService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PlaceOrderAsync(Order order)
    {
        // persist order...
        await _eventBus.PublishAsync(new OrderPlacedEvent(order.Id, order.UserId, order.TotalAmount));
    }
}
```

`IEventBus` resolves the configured provider (`InMemoryEventBus`, `RabbitMqEventBus` or `KafkaEventBus`) and goes through the `DiagnosticsEventBus` decorator when enabled.

### Providers

| Provider | Package | DI Method |
|----------|---------|-----------|
| InMemory | FlowCore (built-in) | `AddFlowCore()` |
| RabbitMQ | FlowCore.RabbitMQ | `.AddRabbitMQ()` |
| Kafka | FlowCore.Kafka | `.AddKafka()` |

### Consumption

Consumers are `BackgroundService` instances registered automatically:
- `RabbitMqConsumerWorker` / `KafkaConsumerWorker`
- Each message goes through: envelope validation → Inbox check (idempotency) → Handler resolution → Retry with DLQ on failure

---

## 📝 Best Practices

1. **Use past tense names** - UserCreated, OrderPlaced, PaymentProcessed
2. **Keep events immutable** - do not modify events after creation
3. **Include relevant data** - information needed by handlers
4. **Use multiple handlers** - for different reactive actions
5. **Prefer IEventBus for cross-service** - use PublishAsync for intra-process events
