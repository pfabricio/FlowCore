using FlowCore.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowCore.Hosting;

internal sealed class BootstrapHostedService : IHostedService
{
    private readonly IBootstrapCoordinator _coordinator;
    private readonly ILogger<BootstrapHostedService> _logger;

    public BootstrapHostedService(
        IBootstrapCoordinator coordinator,
        ILogger<BootstrapHostedService> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _coordinator.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FlowCore Bootstrap failed");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _coordinator.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FlowCore Shutdown failed");
        }
    }
}
