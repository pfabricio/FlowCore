using FlowCore.Abstractions;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Hosting;
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
        if (module.Manifest is not null)
            RegisterManifest(module.Manifest);
        module.Configure(this);
        return this;
    }

    public IFlowCoreBuilder RegisterManifest(IModuleManifest manifest)
    {
        Services.AddSingleton(manifest);
        return this;
    }

    public IFlowCoreBuilder AddHealthCheck<T>() where T : class, IHealthCheck
    {
        Services.AddSingleton<IHealthCheck, T>();
        return this;
    }

    public IFlowCoreBuilder AddHostedWorker<T>() where T : class, IHostedWorker
    {
        Services.AddSingleton<IHostedWorker, T>();
        return this;
    }
}
