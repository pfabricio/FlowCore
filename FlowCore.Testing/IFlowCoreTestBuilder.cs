using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Testing;

public interface IFlowCoreTestBuilder
{
    IServiceProvider Build();
}
