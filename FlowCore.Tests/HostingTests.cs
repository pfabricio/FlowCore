using FluentAssertions;
using FlowCore.Abstractions;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Hosting;
using FlowCore.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class HostingTests
{
    [Fact]
    public void HostedWorkerManager_ShouldReturnRegisteredWorkers()
    {
        var worker1 = new FakeHostedWorker();
        var worker2 = new FakeHostedWorker();

        var manager = new HostedWorkerManager([worker1, worker2]);

        manager.Workers.Should().HaveCount(2);
        manager.Workers.Should().Contain(w => w.Name == "FakeWorker");
    }

    [Fact]
    public void AddHostedWorker_ShouldRegisterInDi()
    {
        var services = new ServiceCollection();
        var builder = new FlowCoreBuilder(services);

        builder.AddHostedWorker<FakeHostedWorker>();

        var provider = services.BuildServiceProvider();
        var workers = provider.GetServices<IHostedWorker>();

        workers.Should().Contain(w => w is FakeHostedWorker);
    }

    [Fact]
    public async Task BootstrapCoordinator_ShouldStartAndStopProviders()
    {
        var providerMock = new Mock<IMessageProvider>();
        providerMock.SetupGet(p => p.Name).Returns("TestProvider");
        providerMock.Setup(p => p.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());
        providerMock.Setup(p => p.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var providerRegistry = new ProviderRegistry([providerMock.Object]);
        var moduleRegistry = new ModuleRegistry([]);
        var coordinator = new BootstrapCoordinator(
            providerRegistry,
            moduleRegistry,
            NullLogger<BootstrapCoordinator>.Instance);

        await coordinator.StartAsync();

        providerMock.Verify(p => p.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        providerMock.Verify(p => p.StopAsync(It.IsAny<CancellationToken>()), Times.Never);

        await coordinator.StopAsync();

        providerMock.Verify(p => p.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BootstrapCoordinator_ShouldValidateModuleCompatibility()
    {
        var providerRegistry = new ProviderRegistry([]);
        var incompatibleManifest = new ModuleManifest("OldModule", new Version(1, 0, 0),
            capabilities: [], minimumFlowCoreVersion: new Version(99, 0, 0));
        var moduleRegistry = new ModuleRegistry([incompatibleManifest]);
        var coordinator = new BootstrapCoordinator(
            providerRegistry,
            moduleRegistry,
            NullLogger<BootstrapCoordinator>.Instance);

        var act = async () => await coordinator.StartAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires FlowCore v99.0.0*");
    }

    [Fact]
    public async Task BootstrapCoordinator_ShouldSkipNullMinVersion()
    {
        var providerMock = new Mock<IMessageProvider>();
        providerMock.SetupGet(p => p.Name).Returns("TestProvider");
        providerMock.Setup(p => p.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var providerRegistry = new ProviderRegistry([providerMock.Object]);
        var manifest = new ModuleManifest("AnyModule", new Version(1, 0, 0), []);
        var moduleRegistry = new ModuleRegistry([manifest]);
        var coordinator = new BootstrapCoordinator(
            providerRegistry,
            moduleRegistry,
            NullLogger<BootstrapCoordinator>.Instance);

        await coordinator.StartAsync();

        providerMock.Verify(p => p.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void PluginModule_ShouldBeDiscoverable()
    {
        var module = new TestPluginModule();

        module.Should().BeAssignableTo<IFlowCoreModule>();
        module.Manifest.Should().NotBeNull();
        module.Manifest.Name.Should().Be("TestPlugin");
    }
}

public class FakeHostedWorker : IHostedWorker
{
    public string Name => "FakeWorker";

    public ValueTask StartAsync(CancellationToken cancellationToken = default) => default;
    public ValueTask StopAsync(CancellationToken cancellationToken = default) => default;
}

public class TestPluginModule : Abstractions.PluginModule
{
    public override IModuleManifest Manifest =>
        CreateManifest("TestPlugin", new Version(1, 0, 0), new Version(2, 0, 0));

    public override void Configure(IFlowCoreBuilder builder) { }
}
