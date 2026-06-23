using FluentAssertions;
using FlowCore.Core.Interfaces;
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
        services.AddFlowCoreTransactions();
        var provider = services.BuildServiceProvider();

        var resolver = provider.GetService<IDbContextResolver>();
        resolver.Should().NotBeNull();
    }
}
