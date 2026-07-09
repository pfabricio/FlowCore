namespace FlowCore.Hosting;

public interface IHostedWorkerManager
{
    IReadOnlyCollection<IHostedWorker> Workers { get; }
}
