using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class ProviderRegistry : IProviderRegistry
{
    public IReadOnlyCollection<IMessageProvider> Providers { get; }

    public ProviderRegistry(IEnumerable<IMessageProvider> providers)
    {
        Providers = providers.ToList().AsReadOnly();
    }
}
