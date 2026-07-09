using System.Collections.Immutable;
using FlowCore.Core.Interfaces;

namespace FlowCore.Core;

internal sealed class ModuleRegistry : IModuleRegistry
{
    public IReadOnlyCollection<IModuleManifest> Modules { get; }

    public ModuleRegistry(IEnumerable<IModuleManifest> modules)
    {
        Modules = modules.ToImmutableArray();
    }
}
