using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using Microsoft.Extensions.Logging;

namespace FlowCore.Hosting;

internal sealed class BootstrapCoordinator : IBootstrapCoordinator
{
    private static readonly Version FlowCoreVersion = typeof(BootstrapCoordinator).Assembly.GetName().Version ?? new Version(2, 2, 0);

    private readonly IProviderRegistry _providerRegistry;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ILogger<BootstrapCoordinator> _logger;

    public BootstrapCoordinator(
        IProviderRegistry providerRegistry,
        IModuleRegistry moduleRegistry,
        ILogger<BootstrapCoordinator> logger)
    {
        _providerRegistry = providerRegistry;
        _moduleRegistry = moduleRegistry;
        _logger = logger;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FlowCore Bootstrap: starting initialization");

        EventTypeResolver.Warmup(AppDomain.CurrentDomain.GetAssemblies());

        ValidateModuleCompatibility();

        _logger.LogInformation("FlowCore Bootstrap: loaded {Count} module(s)", _moduleRegistry.Modules.Count);

        foreach (var provider in _providerRegistry.Providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Starting provider {Provider}", provider.Name);
            await provider.StartAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Provider {Provider} started", provider.Name);
        }

        _logger.LogInformation("FlowCore Bootstrap: initialization complete");
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FlowCore Shutdown: starting shutdown");

        foreach (var provider in _providerRegistry.Providers.Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Stopping provider {Provider}", provider.Name);
            await provider.StopAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Provider {Provider} stopped", provider.Name);
        }

        _logger.LogInformation("FlowCore Shutdown: complete");
    }

    private void ValidateModuleCompatibility()
    {
        foreach (var manifest in _moduleRegistry.Modules)
        {
            if (manifest.MinimumFlowCoreVersion is null)
                continue;

            if (FlowCoreVersion < manifest.MinimumFlowCoreVersion)
            {
                _logger.LogError(
                    "Module {Module} v{ModuleVersion} requires FlowCore v{MinVersion} or higher, but current version is v{CurrentVersion}",
                    manifest.Name,
                    manifest.Version,
                    manifest.MinimumFlowCoreVersion,
                    FlowCoreVersion);

                throw new InvalidOperationException(
                    $"Module '{manifest.Name}' requires FlowCore v{manifest.MinimumFlowCoreVersion} or higher. " +
                    $"Current version is v{FlowCoreVersion}.");
            }

            _logger.LogInformation(
                "Module {Module} v{ModuleVersion} is compatible with FlowCore v{CurrentVersion}",
                manifest.Name,
                manifest.Version,
                FlowCoreVersion);
        }
    }
}
