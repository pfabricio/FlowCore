# Events — FlowCore

> Como criar e utilizar eventos e o EventBus no FlowCore.

---

## 📖 Visão Geral

Eventos representam notificações sobre algo que aconteceu no sistema. No FlowCore, eventos implementam a interface `IEvent`.

Há duas formas de publicar eventos:
- **InMemory** via `IFlowMediator.PublishAsync()` — eventos são processados no mesmo processo
- **Distribuído** via `IEventBus` — eventos podem ser publicados em providers externos (RabbitMQ, Kafka) com suporte a Outbox, Retry e Inbox

---

## 🎯 Criando um Event

```csharp
public record UserCreatedEvent(Guid UserId, string UserName) : IEvent;
public record OrderPlacedEvent(Guid OrderId, Guid UserId, decimal TotalAmount) : IEvent;
```

---

## 🔧 Criando um Handler

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

## 🚀 Uso com IFlowMediator (InMemory)

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

## 🚀 Uso com IEventBus (Distribuído)

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
        // persistir pedido...
        await _eventBus.PublishAsync(new OrderPlacedEvent(order.Id, order.UserId, order.TotalAmount));
    }
}
```

`IEventBus` resolve o provider configurado (`InMemoryEventBus`, `RabbitMqEventBus` ou `KafkaEventBus`) e passa pelo `DiagnosticsEventBus` decorator quando habilitado.

### Providers

| Provider | Package | Método DI |
|----------|---------|-----------|
| InMemory | FlowCore (built-in) | `AddFlowCore()` |
| RabbitMQ | FlowCore.RabbitMQ | `.AddRabbitMQ()` |
| Kafka | FlowCore.Kafka | `.AddKafka()` |

### Consumo

Os consumers são `BackgroundService` registrados automaticamente:
- `RabbitMqConsumerWorker` / `KafkaConsumerWorker`
- Cada mensagem passa por: validação de envelope → verificação Inbox (idempotência) → Resolução do handler → Retry com DLQ em caso de falha

---

## 📝 Melhores Práticas

1. **Use nomes no passado** - UserCreated, OrderPlaced, PaymentProcessed
2. **Mantenha eventos imutáveis** - não modifique eventos após criação
3. **Inclua dados relevantes** - informações necessárias para handlers
4. **Use múltiplos handlers** - para diferentes ações reativas
5. **Prefira IEventBus para cross-service** - use PublishAsync para eventos intra-processo
