using FluentAssertions;
using FlowCore.Discovery;
using FlowCore.Tests.Helpers;
using Xunit;

namespace FlowCore.Tests;

public class HandlerDiscoveryTests
{
    [Fact]
    public void Discover_WithReflection_ShouldFindHandlers()
    {
        var discovery = new HandlerDiscovery();

        var registry = discovery.Discover(typeof(TestCommandHandler).Assembly);

        registry.Handlers.Should().NotBeEmpty();
        registry.Handlers.Should().Contain(h => h.RequestType == typeof(TestCommand));
        registry.Handlers.Should().Contain(h => h.RequestType == typeof(TestQuery));
    }

    [Fact]
    public void Discover_ShouldDetectCommandHandler()
    {
        var discovery = new HandlerDiscovery();

        var registry = discovery.Discover(typeof(TestCommandHandler).Assembly);

        var cmdHandler = registry.Handlers
            .Should().Contain(h => h.RequestType == typeof(TestCommand)).Subject;
        cmdHandler.HandlerType.Should().Be(typeof(TestCommandHandler));
        cmdHandler.ResponseType.Should().Be(typeof(string));
    }

    [Fact]
    public void Discover_ShouldDetectAllHandlerKinds()
    {
        var discovery = new HandlerDiscovery();
        var assembly = typeof(TestCommandHandler).Assembly;

        var registry = discovery.Discover(assembly);

        registry.Handlers.Should().Contain(h => h.Kind == HandlerKind.Command);
        registry.Handlers.Should().Contain(h => h.Kind == HandlerKind.Query);
        registry.Handlers.Should().Contain(h => h.HandlerType == typeof(TestCommandNoReturnHandler));
    }

    [Fact]
    public void Discover_WithoutAssemblies_ShouldReturnEmpty()
    {
        var discovery = new HandlerDiscovery();

        var registry = discovery.Discover();

        registry.Handlers.Should().BeEmpty();
    }
}
