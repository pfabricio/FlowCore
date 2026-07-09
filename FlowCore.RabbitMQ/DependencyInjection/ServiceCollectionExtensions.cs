using FlowCore.Core.Interfaces;
using FlowCore.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.RabbitMQ.DependencyInjection;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<RabbitMQOptions> configure)
    {
        var options = new RabbitMQOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<RabbitMqConnectionManager>();
        services.AddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        services.AddHostedService<RabbitMqConsumerWorker>();

        return services;
    }
}