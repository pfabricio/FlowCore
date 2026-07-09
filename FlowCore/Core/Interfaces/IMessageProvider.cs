namespace FlowCore.Core.Interfaces;

public interface IMessageProvider
{
    string Name { get; }
    ValueTask StartAsync(CancellationToken cancellationToken = default);
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
