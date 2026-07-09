using System.Collections.Immutable;

namespace FlowCore.Hosting;

internal sealed class HostedWorkerManager : IHostedWorkerManager
{
    public IReadOnlyCollection<IHostedWorker> Workers { get; }

    public HostedWorkerManager(IEnumerable<IHostedWorker> workers)
    {
        Workers = workers.ToImmutableArray();
    }
}
