using FlowCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore;

internal sealed class FlowCoreBuilder : IFlowCoreBuilder
{
    public IServiceCollection Services { get; }

    public FlowCoreBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IFlowCoreBuilder AddModule<TModule>() where TModule : class, IFlowCoreModule, new()
    {
        var module = new TModule();
        module.Configure(this);
        return this;
    }
}
