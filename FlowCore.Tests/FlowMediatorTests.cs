using FluentAssertions;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using FlowCore.Messaging;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class FlowMediatorTests
{

    private ServiceProvider CreateServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<DispatcherCache>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<IHandlerResolver, DiHandlerResolver>();
        services.AddScoped<IFlowMediator, FlowMediator>();
        services.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>();
        services.AddScoped<ICommandHandler<TestCommandNoReturn, Unit>, TestCommandNoReturnHandler>();
        services.AddScoped<ICommandHandler<FailingCommand, string>, FailingCommandHandler>();
        services.AddScoped<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        services.AddScoped<IEventHandler<TestEvent>, TestEventHandler>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_WithCommand_ShouldReturnResult()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.SendAsync(new TestCommand("World"));

        result.Should().Be("Hello World");
    }

    [Fact]
    public async Task SendAsync_WithCommandNoReturn_ShouldNotThrow()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var act = () => mediator.SendAsync(new TestCommandNoReturn("Test"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WithFailingCommand_ShouldThrowException()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var act = () => mediator.SendAsync(new FailingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler failed");
    }

    [Fact]
    public async Task QueryAsync_WithQuery_ShouldReturnResult()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.QueryAsync(new TestQuery("123"));

        result.Should().Be("Result for 123");
    }

    [Fact]
    public async Task PublishAsync_WithEvent_ShouldNotifyAllHandlers()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddSingleton(handlerMock.Object);
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        await mediator.PublishAsync(new TestEvent("test-data"));

        handlerMock.Verify(
            x => x.HandleAsync(It.Is<TestEvent>(e => e.Data == "test-data"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_ShouldNotifyAll()
    {
        var secondHandlerMock = new Mock<IEventHandler<TestEvent>>();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddSingleton(secondHandlerMock.Object);
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        await mediator.PublishAsync(new TestEvent("data"));

        secondHandlerMock.Verify(
            x => x.HandleAsync(It.Is<TestEvent>(e => e.Data == "data"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerThrows_ShouldPropagateException()
    {
        var failingHandler = new Mock<IEventHandler<TestEvent>>();
        failingHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Event handler failed"));

        using var provider = CreateServiceProvider(services =>
        {
            services.AddSingleton(failingHandler.Object);
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var act = () => mediator.PublishAsync(new TestEvent("data"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Event handler failed");
    }
}
