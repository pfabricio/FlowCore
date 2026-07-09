using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Messaging;
using FlowCore.Pipeline.Behaviors;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFlowCore_ShouldRegisterMediator()
    {
        var services = new ServiceCollection();
        services.AddFlowCore();
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetService<IFlowMediator>();
        mediator.Should().NotBeNull();
        mediator.Should().BeOfType<FlowMediator>();
    }

    [Fact]
    public void AddFlowCore_ShouldRegisterEventBus()
    {
        var services = new ServiceCollection();
        services.AddFlowCore();
        var provider = services.BuildServiceProvider();

        var eventBus = provider.GetService<IEventBus>();
        eventBus.Should().NotBeNull();
        eventBus.Should().BeOfType<DiagnosticsEventBus>();
        var innerField = typeof(DiagnosticsEventBus).GetField("_inner",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var innerBus = innerField?.GetValue(eventBus);
        innerBus.Should().BeOfType<InMemoryEventBus>();

        var cache = provider.GetService<DispatcherCache>();
        cache.Should().NotBeNull();

        var serializer = provider.GetService<IMessageSerializer>();
        serializer.Should().NotBeNull();
        serializer.Should().BeOfType<SystemTextJsonSerializer>();
    }

    [Fact]
    public void AddFlowCore_ShouldRegisterBehaviors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddFlowCore();
        var provider = services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior<TestQuery, string>>().ToList();
        behaviors.Should().NotBeEmpty();
        behaviors.Should().Contain(b => b is LoggingBehavior<TestQuery, string>);
        behaviors.Should().Contain(b => b is ValidationBehavior<TestQuery, string>);
        behaviors.Should().Contain(b => b is CachingBehavior<TestQuery, string>);
        behaviors.Should().Contain(b => b is EventDispatcherBehavior<TestQuery, string>);
    }

    [Fact]
    public void AddFlowCore_ShouldNotRegisterTransactionBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddFlowCore();
        var provider = services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior<TestQuery, string>>().ToList();
        behaviors.Should().NotContain(b => b.GetType().Name.Contains("TransactionScope"));
    }

    [Fact]
    public void AddFlowCoreTransactions_ShouldRegisterTransactionBehavior()
    {
        var services = new ServiceCollection();
        services.AddFlowCore().AddFlowCoreTransactions();
        var provider = services.BuildServiceProvider();

        var resolver = provider.GetService<IDbContextResolver>();
        resolver.Should().NotBeNull();
    }
}
