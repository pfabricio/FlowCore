using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Abstractions;

public interface IFlowCoreBuilder
{
    IServiceCollection Services { get; }

    IFlowCoreBuilder AddModule<TModule>() where TModule : class, IFlowCoreModule, new();

    IFlowCoreBuilder RegisterManifest(IModuleManifest manifest);

    IFlowCoreBuilder AddHealthCheck<T>() where T : class, IHealthCheck;

    IFlowCoreBuilder AddHostedWorker<T>() where T : class, IHostedWorker;
}
