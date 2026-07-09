namespace FlowCore.Core.Interfaces;

public interface IBootstrapCoordinator
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
