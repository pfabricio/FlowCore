namespace FlowCore.Core.Interfaces;

public interface IModuleManifest
{
    string Name { get; }

    Version Version { get; }

    Version? MinimumFlowCoreVersion { get; }

    IReadOnlyCollection<string> Capabilities { get; }

    IReadOnlyCollection<Type> Dependencies { get; }
}
