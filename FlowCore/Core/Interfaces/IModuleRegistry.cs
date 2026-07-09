namespace FlowCore.Core.Interfaces;

public interface IModuleRegistry
{
    IReadOnlyCollection<IModuleManifest> Modules { get; }
}
