using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Abstractions;

public interface IFlowCoreBuilder
{
    IServiceCollection Services { get; }

    IFlowCoreBuilder AddModule<TModule>() where TModule : class, IFlowCoreModule, new();
}
