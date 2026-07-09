using FlowCore.Abstractions;
using FlowCore.Core.Interfaces;
using FlowCore.Kafka.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Kafka.DependencyInjection;

public static class KafkaServiceCollectionExtensions
{
    public static IFlowCoreBuilder AddKafka(this IFlowCoreBuilder builder, Action<KafkaOptions> configure)
    {
        var options = new KafkaOptions();
        configure(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<KafkaEventBus>();
        builder.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());
        builder.Services.AddSingleton<IMessageProvider>(sp => sp.GetRequiredService<KafkaEventBus>());
        builder.Services.AddHostedService<KafkaConsumerWorker>();

        return builder;
    }
}