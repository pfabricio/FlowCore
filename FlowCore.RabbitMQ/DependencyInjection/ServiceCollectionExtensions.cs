using FlowCore.Abstractions;
using FlowCore.Core.Interfaces;
using FlowCore.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.RabbitMQ.DependencyInjection;

public static class RabbitMQServiceCollectionExtensions
{
    public static IFlowCoreBuilder AddRabbitMQ(this IFlowCoreBuilder builder, Action<RabbitMQOptions> configure)
    {
        var options = new RabbitMQOptions();
        configure(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<RabbitMqConnectionManager>();
        builder.Services.AddSingleton<RabbitMqEventBus>();
        builder.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        builder.Services.AddSingleton<IMessageProvider>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        builder.Services.AddHostedService<RabbitMqConsumerWorker>();

        return builder;
    }
}