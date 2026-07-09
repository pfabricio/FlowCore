using FlowCore.Core.Interfaces;
using FlowCore.Kafka.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Kafka.DependencyInjection;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services, Action<KafkaOptions> configure)
    {
        var options = new KafkaOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<KafkaEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());
        services.AddHostedService<KafkaConsumerWorker>();

        return services;
    }
}