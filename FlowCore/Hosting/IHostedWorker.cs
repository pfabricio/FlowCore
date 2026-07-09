namespace FlowCore.Hosting;

public interface IHostedWorker
{
    string Name { get; }

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
