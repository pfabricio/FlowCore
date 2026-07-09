namespace FlowCore.Core.Interfaces;

public interface IProviderRegistry
{
    IReadOnlyCollection<IMessageProvider> Providers { get; }
}
