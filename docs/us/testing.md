# Testing — FlowCore

> How to test handlers, behaviors and applications in FlowCore.

---

## 📖 Overview

FlowCore is designed to be easily testable. Handlers and behaviors can be tested in isolation using mocks and simulated dependencies. For integration tests, the `FlowCore.Testing` package provides reusable infrastructure.

---

## 🎯 Testing Handlers

### Unit Test for Command Handler

```csharp
public class CreateUserCommandHandlerTests
{
    private readonly Mock<MyDbContext> _contextMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _contextMock = new Mock<MyDbContext>();
        _handler = new CreateUserCommandHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateUser_ReturnsUserId()
    {
        var command = new CreateUserCommand("John", "john@email.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        _contextMock.Verify(x => x.Users.Add(It.IsAny<User>()), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Unit Test for Query Handler

```csharp
public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<MyDbContext> _contextMock;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _contextMock = new Mock<MyDbContext>();
        _handler = new GetUserByIdQueryHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUser_WhenExists()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John", Email = "john@email.com" };

        _contextMock.Setup(x => x.Users.FindAsync(userId))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }
}
```

---

## 🔧 Testing Behaviors

### Validation Behavior Test

```csharp
public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldPassValidation_WhenValidCommand()
    {
        var validators = new List<IValidator<CreateUserCommand>> { new CreateUserCommandValidator() };
        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(validators);
        var command = new CreateUserCommand("John", "john@email.com");
        RequestHandlerDelegate<Guid> next = () => Task.FromResult(Guid.NewGuid());

        var result = await behavior.Handle(command, next, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenInvalidCommand()
    {
        var validators = new List<IValidator<CreateUserCommand>> { new CreateUserCommandValidator() };
        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(validators);
        var command = new CreateUserCommand("", "invalid-email");
        RequestHandlerDelegate<Guid> next = () => Task.FromResult(Guid.NewGuid());

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => behavior.Handle(command, next, CancellationToken.None));
    }
}
```

---

## 🚀 Testing with FlowCore.Testing

The `FlowCore.Testing` package provides `FakeEventBus`, `FakeClock` and `FlowCoreTestBuilder` for integration tests without external infrastructure.

### Installation

```bash
dotnet add package FlowCore.Testing --version 2.2.1
```

### Integration Test with FakeEventBus

```csharp
public class OrderServiceTests
{
    [Fact]
    public async Task PlaceOrder_ShouldPublishEvent()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOrderService, OrderService>();
        services.AddDbContext<MyDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        var builder = services.CreateTestBuilder();
        var provider = builder.Build();

        var orderService = provider.GetRequiredService<IOrderService>();
        var fakeBus = provider.GetFakeEventBus();

        await orderService.PlaceOrderAsync("John", 100);

        var events = fakeBus.PublishedOfType<OrderPlacedEvent>();
        Assert.Single(events);
        Assert.Equal(100, events.Single().Total);
    }
}
```

### FakeClock

```csharp
[Fact]
public void Schedule_WithFakeClock_ShouldAdvanceTime()
{
    var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    clock.Advance(TimeSpan.FromHours(2));

    Assert.Equal(2, clock.UtcNow.Hour);
}
```

---

## 📝 Best Practices

1. **Test in isolation** — each handler and behavior should be tested independently
2. **Use mocks** — to simulate external dependencies
3. **Test error scenarios** — ensure exceptions are handled correctly
4. **Use FlowCore.Testing** — for integration tests with FakeEventBus
5. **Keep tests clean** — arrange, act, assert should be clear
