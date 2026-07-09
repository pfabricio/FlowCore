using FlowCore.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Abstractions;

public interface IFlowCoreModule
{
    IModuleManifest? Manifest => null;

    void Configure(IFlowCoreBuilder builder);
}
