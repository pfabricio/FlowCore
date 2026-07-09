using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FlowCore.Core.Interfaces;

namespace FlowCore.Saga;

internal sealed class SagaEventListener : IEventHandler<IEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SagaEventListener(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var coordinator = scope.ServiceProvider.GetRequiredService<SagaCoordinator>();
        await coordinator.HandleEventAsync(@event, cancellationToken);
    }
}