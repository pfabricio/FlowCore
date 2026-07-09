using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Testing;

internal sealed class FlowCoreTestBuilder : IFlowCoreTestBuilder
{
    private readonly IServiceCollection _services;

    public FlowCoreTestBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public IServiceProvider Build()
    {
        return _services.BuildServiceProvider();
    }
}
