using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Abstractions;

public interface IFlowCoreModule
{
    void Configure(IFlowCoreBuilder builder);
}
