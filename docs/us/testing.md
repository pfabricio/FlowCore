# Testing — FlowCore

> How to test handlers and behaviors in FlowCore.

---

## 📖 Overview

FlowCore is designed to be easily testable. Handlers and behaviors can be tested in isolation using mocks and simulated dependencies.

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
        // Arrange
        var command = new CreateUserCommand("John", "john@email.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
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
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John", Email = "john@email.com" };

        _contextMock.Setup(x => x.Users.FindAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("John", result.Name);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _contextMock.Setup(x => x.Users.FindAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None));
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
        // Arrange
        var validators = new List<IValidator<CreateUserCommand>>
        {
            new CreateUserCommandValidator()
        };
        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(validators);
        var command = new CreateUserCommand("John", "john@email.com");
        RequestHandlerDelegate<Guid> next = () => Task.FromResult(Guid.NewGuid());

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenInvalidCommand()
    {
        // Arrange
        var validators = new List<IValidator<CreateUserCommand>>
        {
            new CreateUserCommandValidator()
        };
        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(validators);
        var command = new CreateUserCommand("", "invalid-email");
        RequestHandlerDelegate<Guid> next = () => Task.FromResult(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => behavior.Handle(command, next, CancellationToken.None));
    }
}
```

### Logging Behavior Test

```csharp
public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldLogRequests()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<CreateUserCommand, Guid>>>();
        var behavior = new LoggingBehavior<CreateUserCommand, Guid>(loggerMock.Object);
        var command = new CreateUserCommand("John", "john@email.com");
        RequestHandlerDelegate<Guid> next = () => Task.FromResult(Guid.NewGuid());

        // Act
        await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

---

## 🚀 Testing with Mediator

### Integration Test

```csharp
public class FlowMediatorTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IFlowMediator _mediator;

    public FlowMediatorTests()
    {
        var services = new ServiceCollection();
        services.AddFlowCore();
        services.AddDbContext<MyDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IFlowMediator>();
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteHandler()
    {
        // Arrange
        var command = new CreateUserCommand("John", "john@email.com");

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
    }
}
```

---

## 📝 Best Practices

1. **Test in isolation** - each handler and behavior should be tested independently
2. **Use mocks** - to simulate external dependencies
3. **Test error scenarios** - ensure exceptions are handled correctly
4. **Use realistic test data** - to ensure tests reflect real usage
5. **Keep tests clean** - arrange, act, assert should be clear