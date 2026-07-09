using FlowCore.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Testing;

public static class FlowCoreTestingExtensions
{
    public static IFlowCoreTestBuilder CreateTestBuilder(this IServiceCollection services)
    {
        services.AddFlowCore();
        services.AddSingleton<FakeEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<FakeEventBus>());

        return new FlowCoreTestBuilder(services);
    }

    public static FakeEventBus GetFakeEventBus(this IServiceProvider services)
    {
        return services.GetRequiredService<FakeEventBus>();
    }
}
