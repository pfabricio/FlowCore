using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class EventBusTests
{
    private ServiceProvider CreateServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<DispatcherCache>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddScoped<IEventHandler<TestEvent>, TestEventHandler>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task PublishAsync_Generic_ShouldInvokeHandlers()
    {
        var handler = new TestEventHandler();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IEventHandler<TestEvent>>(_ => handler);
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new TestEvent("generic-test"));

        handler.ReceivedEvents.Should().ContainSingle(e => e == "generic-test");
    }

    [Fact]
    public async Task PublishAsync_NonGeneric_ShouldInvokeHandlers()
    {
        var handler = new TestEventHandler();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IEventHandler<TestEvent>>(_ => handler);
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new TestEvent("non-generic-test"));

        handler.ReceivedEvents.Should().ContainSingle(e => e == "non-generic-test");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_ShouldInvokeAll()
    {
        var handler1 = new TestEventHandler();
        var handler2 = new Mock<IEventHandler<TestEvent>>();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IEventHandler<TestEvent>>(_ => handler1);
            services.AddScoped(provider => handler2.Object);
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new TestEvent("multi"));

        handler1.ReceivedEvents.Should().ContainSingle(e => e == "multi");
        handler2.Verify(x => x.HandleAsync(
            It.Is<TestEvent>(e => e.Data == "multi"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenNoHandlers_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DispatcherCache>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var act = () => bus.PublishAsync(new TestEvent("no-handlers"));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithFailingHandler_ShouldPropagate()
    {
        var failingHandler = new Mock<IEventHandler<TestEvent>>();
        failingHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bus fail"));

        using var provider = CreateServiceProvider(services =>
        {
            services.AddSingleton(failingHandler.Object);
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var act = () => bus.PublishAsync(new TestEvent("fail"));
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bus fail");
    }

    [Fact]
    public void Name_ShouldReturnInMemory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DispatcherCache>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        using var provider = services.BuildServiceProvider();
        var bus = (InMemoryEventBus)provider.GetRequiredService<IEventBus>();
        bus.Name.Should().Be("InMemory");
    }
}
