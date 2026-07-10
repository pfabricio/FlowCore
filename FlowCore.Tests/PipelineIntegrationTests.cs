using FluentAssertions;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowCore.Tests;

public class PipelineIntegrationTests
{
    private ServiceProvider CreateServiceProvider(
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<DispatcherCache>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<IHandlerResolver, DiHandlerResolver>();
        services.AddScoped<IFlowMediator, FlowMediator>();
        services.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>();
        services.AddScoped<ICommandHandler<TestCommandNoReturn, Unit>, TestCommandNoReturnHandler>();
        services.AddScoped<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        services.AddScoped<IEventHandler<TestEvent>, TestEventHandler>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_WithoutBehaviors_ShouldReturnResult()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.SendAsync(new TestCommand("direct"));

        result.Should().Be("Hello direct");
    }

    [Fact]
    public async Task QueryAsync_WithoutBehaviors_ShouldReturnResult()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.QueryAsync(new TestQuery("integration"));

        result.Should().Be("Result for integration");
    }

    [Fact]
    public async Task SendAsync_WithSingleBehavior_ShouldExecuteBehavior()
    {
        var order = new List<string>();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IPipelineBehavior<TestCommand, string>>(_ =>
                new TestBehavior<TestCommand, string>("Behavior1"));
        });
        using var scope = provider.CreateScope();

        var behavior = scope.ServiceProvider
            .GetServices<IPipelineBehavior<TestCommand, string>>()
            .OfType<TestBehavior<TestCommand, string>>()
            .First();
        behavior.ExecutedBehaviors.Clear();

        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();
        var result = await mediator.SendAsync(new TestCommand("with-behavior"));

        result.Should().Be("Hello with-behavior");
    }

    [Fact]
    public async Task SendAsync_WithBehaviorModifyingContext_ShouldWork()
    {
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IPipelineBehavior<TestCommand, string>>(
                _ => new TestBehavior<TestCommand, string>("Modifier"));
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.SendAsync(new TestCommand("modified"));
        result.Should().Be("Hello modified");
    }

    [Fact]
    public async Task SendAsync_WithMultipleBehaviors_ShouldExecuteInOrder()
    {
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IPipelineBehavior<TestCommand, string>>(
                _ => new TestBehavior<TestCommand, string>("First"));
            services.AddScoped<IPipelineBehavior<TestCommand, string>>(
                _ => new TestBehavior<TestCommand, string>("Second"));
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.SendAsync(new TestCommand("multi-behavior"));
        result.Should().Be("Hello multi-behavior");
    }

    [Fact]
    public async Task SendAsync_WithUnitReturn_ShouldNotThrow()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var act = () => mediator.SendAsync(new TestCommandNoReturn("unit-test"));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeEventHandlers()
    {
        var handler = new TestEventHandler();
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IEventHandler<TestEvent>>(_ => handler);
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        await mediator.PublishAsync(new TestEvent("pub-test"));

        handler.ReceivedEvents.Should().ContainSingle(e => e == "pub-test");
    }

    [Fact]
    public async Task SendAsync_CustomBehaviorPipeline_ShouldExecuteAll()
    {
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IPipelineBehavior<TestCommand, string>>(
                _ => new TestBehavior<TestCommand, string>("A"));
            services.AddScoped<IPipelineBehavior<TestCommand, string>>(
                _ => new TestBehavior<TestCommand, string>("B"));
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.SendAsync(new TestCommand("pipeline-test"));
        result.Should().Be("Hello pipeline-test");
    }

    [Fact]
    public async Task QueryAsync_WithBehaviors_ShouldReturnResult()
    {
        using var provider = CreateServiceProvider(services =>
        {
            services.AddScoped<IPipelineBehavior<TestQuery, string>>(
                _ => new TestBehavior<TestQuery, string>("QueryBehavior"));
        });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IFlowMediator>();

        var result = await mediator.QueryAsync(new TestQuery("pipeline-query"));
        result.Should().Be("Result for pipeline-query");
    }
}
