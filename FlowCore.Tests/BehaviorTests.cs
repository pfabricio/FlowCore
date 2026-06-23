using FluentAssertions;
using FluentValidation;
using FlowCore.Core.Interfaces;
using FlowCore.Pipeline.Behaviors;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldLogRequestAndResponse()
    {
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<LoggingBehavior<TestCommand, string>>>();
        var behavior = new LoggingBehavior<TestCommand, string>(loggerMock.Object);

        var response = await behavior.Handle(
            new TestCommand("Test"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        response.Should().Be("result");
        loggerMock.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }
}

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestCommand, string>(Array.Empty<IValidator<TestCommand>>());

        var response = await behavior.Handle(
            new TestCommand("Test"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        response.Should().Be("result");
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ShouldThrowValidationException()
    {
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("Name", "Name is required")
            }));

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { validatorMock.Object });

        var act = () => behavior.Handle(
            new TestCommand("Test"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}

public class CachingBehaviorTests
{
    [Fact]
    public async Task Handle_WithNonCachableQuery_ShouldCallNext()
    {
        var cacheProviderMock = new Mock<ICacheProvider>();
        var behavior = new CachingBehavior<TestQuery, string>(new ServiceCollection()
            .AddSingleton(cacheProviderMock.Object)
            .BuildServiceProvider());

        var response = await behavior.Handle(
            new TestQuery("123"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        response.Should().Be("result");
        cacheProviderMock.Verify(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCachableQuery_CacheHit_ShouldReturnCached()
    {
        var cacheProviderMock = new Mock<ICacheProvider>();
        cacheProviderMock
            .Setup(x => x.GetAsync<string>("test:123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("cached-value");

        var behavior = new CachingBehavior<TestCachableQuery, string>(new ServiceCollection()
            .AddSingleton(cacheProviderMock.Object)
            .BuildServiceProvider());

        var response = await behavior.Handle(
            new TestCachableQuery("123"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        response.Should().Be("cached-value");
    }

    [Fact]
    public async Task Handle_WithCachableQuery_CacheMiss_ShouldCallNextAndCache()
    {
        var cacheProviderMock = new Mock<ICacheProvider>();
        cacheProviderMock
            .Setup(x => x.GetAsync<string>("test:123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var behavior = new CachingBehavior<TestCachableQuery, string>(new ServiceCollection()
            .AddSingleton(cacheProviderMock.Object)
            .BuildServiceProvider());

        var response = await behavior.Handle(
            new TestCachableQuery("123"),
            () => Task.FromResult("new-value"),
            CancellationToken.None);

        response.Should().Be("new-value");
        cacheProviderMock.Verify(
            x => x.SetAsync("test:123", "new-value", TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCachableQuery_NullResponse_ShouldNotCache()
    {
        var cacheProviderMock = new Mock<ICacheProvider>();
        cacheProviderMock
            .Setup(x => x.GetAsync<string>("test:123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var behavior = new CachingBehavior<TestCachableQuery, string>(new ServiceCollection()
            .AddSingleton(cacheProviderMock.Object)
            .BuildServiceProvider());

        var response = await behavior.Handle(
            new TestCachableQuery("123"),
            () => Task.FromResult<string>(null!),
            CancellationToken.None);

        response.Should().BeNull();
        cacheProviderMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutCacheProvider_ShouldCallNext()
    {
        var behavior = new CachingBehavior<TestCachableQuery, string>(new ServiceCollection()
            .BuildServiceProvider());

        var response = await behavior.Handle(
            new TestCachableQuery("123"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        response.Should().Be("result");
    }
}

public class EventDispatcherBehaviorTests
{
    [Fact]
    public async Task Handle_WithNonEventSource_ShouldCallNext()
    {
        var behavior = new EventDispatcherBehavior<TestCommand, string>(new ServiceCollection().BuildServiceProvider(),
            new Mock<Microsoft.Extensions.Logging.ILogger<EventDispatcherBehavior<TestCommand, string>>>().Object);

        var response = await behavior.Handle(
            new TestCommand("Test"),
            () => Task.FromResult("result"),
            CancellationToken.None);

        response.Should().Be("result");
    }
}
