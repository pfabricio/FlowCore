using FluentAssertions;
using FlowCore.Abstractions;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowCore.Tests;

public class ModuleManifestTests
{
    [Fact]
    public void ModuleManifest_ShouldCreateWithValues()
    {
        var version = new Version(1, 0, 0);
        var minVersion = new Version(2, 2, 0);
        var capabilities = new[] { "caching", "routing" };
        var deps = new[] { typeof(IModuleManifest) };

        var manifest = new ModuleManifest("TestModule", version, capabilities, deps, minVersion);

        manifest.Name.Should().Be("TestModule");
        manifest.Version.Should().Be(version);
        manifest.MinimumFlowCoreVersion.Should().Be(minVersion);
        manifest.Capabilities.Should().BeEquivalentTo(capabilities);
        manifest.Dependencies.Should().BeEquivalentTo(deps);
    }

    [Fact]
    public void ModuleManifest_ShouldDefaultToEmptyCollections()
    {
        var manifest = new ModuleManifest("Minimal", new Version(1, 0, 0), []);

        manifest.Capabilities.Should().BeEmpty();
        manifest.Dependencies.Should().BeEmpty();
        manifest.MinimumFlowCoreVersion.Should().BeNull();
    }

    [Fact]
    public void ModuleRegistry_ShouldReturnRegisteredModules()
    {
        var manifest1 = new ModuleManifest("A", new Version(1, 0, 0), []);
        var manifest2 = new ModuleManifest("B", new Version(2, 0, 0), []);
        var registry = new ModuleRegistry([manifest1, manifest2]);

        registry.Modules.Should().HaveCount(2);
        registry.Modules.Should().Contain(m => m.Name == "A");
        registry.Modules.Should().Contain(m => m.Name == "B");
    }

    [Fact]
    public void AddModule_WithManifest_ShouldRegisterIt()
    {
        var services = new ServiceCollection();
        var builder = new FlowCoreBuilder(services);

        builder.AddModule<TestModuleWithManifest>();

        var manifest = services.BuildServiceProvider().GetService<IModuleManifest>();
        manifest.Should().NotBeNull();
        manifest!.Name.Should().Be("TestManifestModule");
    }

    [Fact]
    public void AddModule_WithoutManifest_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new FlowCoreBuilder(services);

        var act = () => builder.AddModule<TestModuleWithoutManifest>();

        act.Should().NotThrow();
    }
}

public class TestModuleWithManifest : IFlowCoreModule
{
    public IModuleManifest Manifest => new ModuleManifest("TestManifestModule", new Version(1, 0, 0), ["test"]);
    public void Configure(IFlowCoreBuilder builder) { }
}

public class TestModuleWithoutManifest : IFlowCoreModule
{
    public void Configure(IFlowCoreBuilder builder) { }
}
