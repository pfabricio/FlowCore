using FlowCore.Core.Interfaces;

namespace FlowCore.Core;

internal sealed class ModuleManifest : IModuleManifest
{
    public string Name { get; }

    public Version Version { get; }

    public Version? MinimumFlowCoreVersion { get; }

    public IReadOnlyCollection<string> Capabilities { get; }

    public IReadOnlyCollection<Type> Dependencies { get; }

    public ModuleManifest(
        string name,
        Version version,
        IReadOnlyCollection<string> capabilities,
        IReadOnlyCollection<Type>? dependencies = null,
        Version? minimumFlowCoreVersion = null)
    {
        Name = name;
        Version = version;
        Capabilities = capabilities;
        Dependencies = dependencies ?? [];
        MinimumFlowCoreVersion = minimumFlowCoreVersion;
    }
}
