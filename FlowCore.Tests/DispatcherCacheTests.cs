using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class DispatcherCacheTests
{
    [Fact]
    public void GetOrCreate_WithEventType_ShouldReturnInvoker()
    {
        var cache = new DispatcherCache();
        var invoker = cache.GetOrCreate(typeof(TestEvent));
        invoker.Should().NotBeNull();
        invoker.Should().BeAssignableTo<IEventHandlerInvoker>();
    }

    [Fact]
    public void GetOrCreate_SameEventType_ShouldReturnCachedInvoker()
    {
        var cache = new DispatcherCache();
        var first = cache.GetOrCreate(typeof(TestEvent));
        var second = cache.GetOrCreate(typeof(TestEvent));
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void GetOrCreate_DifferentEventTypes_ShouldReturnDifferentInvokers()
    {
        var cache = new DispatcherCache();
        var first = cache.GetOrCreate(typeof(TestEvent));
        var second = cache.GetOrCreate(typeof(TestEvent));
        first.Should().BeSameAs(second);
    }

    [Fact]
    public async Task Invoker_ShouldInvokeAllHandlers()
    {
        var handler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestEvent>>(_ => handler);
        using var provider = services.BuildServiceProvider();

        var cache = new DispatcherCache();
        var invoker = cache.GetOrCreate(typeof(TestEvent));

        using var scope = provider.CreateScope();
        await invoker.InvokeAllAsync(scope.ServiceProvider, new TestEvent("data"), default);

        handler.ReceivedEvents.Should().ContainSingle(e => e == "data");
    }

    [Fact]
    public async Task Invoker_WithMultipleHandlers_ShouldInvokeAll()
    {
        var handler1 = new TestEventHandler();
        var handler2 = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestEvent>>(_ => handler1);
        services.AddScoped<IEventHandler<TestEvent>>(_ => handler2);
        using var provider = services.BuildServiceProvider();

        var cache = new DispatcherCache();
        var invoker = cache.GetOrCreate(typeof(TestEvent));

        using var scope = provider.CreateScope();
        await invoker.InvokeAllAsync(scope.ServiceProvider, new TestEvent("multi"), default);

        handler1.ReceivedEvents.Should().ContainSingle(e => e == "multi");
        handler2.ReceivedEvents.Should().ContainSingle(e => e == "multi");
    }

    [Fact]
    public async Task Invoker_WhenHandlerThrows_ShouldPropagate()
    {
        var failingHandler = new Mock<IEventHandler<TestEvent>>();
        failingHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestEvent>>(_ => failingHandler.Object);
        using var provider = services.BuildServiceProvider();

        var cache = new DispatcherCache();
        var invoker = cache.GetOrCreate(typeof(TestEvent));

        using var scope = provider.CreateScope();
        var act = () => invoker.InvokeAllAsync(scope.ServiceProvider, new TestEvent("fail"), default);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fail");
    }

    [Fact]
    public async Task Invoker_ShouldCreateScopeForEachInvocation()
    {
        var handler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestEvent>>(_ => handler);
        using var provider = services.BuildServiceProvider();

        var cache = new DispatcherCache();
        var invoker = cache.GetOrCreate(typeof(TestEvent));

        using var scope = provider.CreateScope();
        await invoker.InvokeAllAsync(scope.ServiceProvider, new TestEvent("scope-test"), default);

        handler.ReceivedEvents.Should().ContainSingle(e => e == "scope-test");
    }
}
