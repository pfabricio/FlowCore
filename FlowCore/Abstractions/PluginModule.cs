using FlowCore.Core;
using FlowCore.Core.Interfaces;

namespace FlowCore.Abstractions;

public abstract class PluginModule : IFlowCoreModule
{
    public abstract IModuleManifest Manifest { get; }

    public abstract void Configure(IFlowCoreBuilder builder);

    protected static IModuleManifest CreateManifest(
        string name,
        Version version,
        Version minimumFlowCoreVersion,
        string[]? capabilities = null,
        Type[]? dependencies = null)
    {
        return new ModuleManifest(
            name,
            version,
            capabilities ?? [],
            dependencies,
            minimumFlowCoreVersion);
    }
}
