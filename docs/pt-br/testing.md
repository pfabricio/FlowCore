# Testing — FlowCore

> Como testar handlers e behaviors no FlowCore.

---

## 📖 Visão Geral

O FlowCore foi projetado para ser facilmente testável. Handlers e behaviors podem ser testados isoladamente usando mocks e dependências simuladas.

---

## 🎯 Testando Handlers

### Teste Unitário de Command Handler

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

### Teste Unitário de Query Handler

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

## 🔧 Testando Behaviors

### Teste de Validation Behavior

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

### Teste de Logging Behavior

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

## 🚀 Testando com o Mediator

### Teste de Integração

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

## 📝 Melhores Práticas

1. **Teste isoladamente** - cada handler e behavior deve ser testado independentemente
2. **Use mocks** - para simular dependências externas
3. **Teste cenários de erro** - garanta que exceções são tratadas corretamente
4. **Use dados de teste realistas** - para garantir que testes refletem uso real
5. **Mantenha testes limpos** - arrange, act, assert devem ser claros