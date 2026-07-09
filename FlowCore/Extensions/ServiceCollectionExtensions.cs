using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlowCore.Abstractions;
using FlowCore;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Discovery;
using FlowCore.Messaging;
using FlowCore.Pipeline.Behaviors;
using FlowCore.Saga;
using FlowCore.Scheduling;
using System.Reflection;

public static class ServiceCollectionExtensions
{
    public static IFlowCoreBuilder AddFlowCore(this IServiceCollection services)
    {
        return services.AddFlowCore(AppDomain.CurrentDomain.GetAssemblies());
    }

    public static IFlowCoreBuilder AddFlowCore(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IActivityFactory>(sp => NullActivityFactory.Instance);
        services.AddSingleton<IMetricRecorder>(sp => NullMetricRecorder.Instance);

        services.AddSingleton<DispatcherCache>();
        services.AddSingleton<IMessageSerializer, SystemTextJsonSerializer>();
        services.AddSingleton<IEventBus>(sp =>
        {
            var inner = new InMemoryEventBus(
                sp.GetRequiredService<DispatcherCache>(),
                sp.GetRequiredService<IServiceProvider>());
            return new DiagnosticsEventBus(
                inner,
                sp.GetRequiredService<IActivityFactory>(),
                sp.GetRequiredService<IMetricRecorder>());
        });
        services.AddSingleton<IRetryPolicy, ImmediateRetryPolicy>();
        services.AddSingleton<IDeadLetterWriter, InMemoryDeadLetterWriter>();
        services.AddSingleton<RetryHandler>();
        services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
        services.AddSingleton<IInboxStore, InMemoryInboxStore>();
        services.AddSingleton<ISagaStore, InMemorySagaStore>();
        services.AddSingleton<SagaCoordinator>();
        services.AddSingleton<IScheduledMessageStore, InMemoryScheduledMessageStore>();
        services.AddSingleton<IMessageScheduler, MessageScheduler>();

        services.TryAddSingleton<IProviderRegistry>(sp =>
        {
            var providers = sp.GetServices<IMessageProvider>();
            return new ProviderRegistry(providers);
        });

        services.TryAddSingleton<IHandlerRegistry>(sp =>
        {
            var discovery = new HandlerDiscovery();
            return discovery.Discover(assemblies);
        });

        services.AddScoped<IFlowMediator, FlowMediator>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventDispatcherBehavior<,>));

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IEventHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        var builder = new FlowCoreBuilder(services);

        return builder;
    }

    public static IFlowCoreBuilder AddFlowCoreTransactions(this IFlowCoreBuilder builder)
    {
        builder.Services.AddScoped<IDbContextResolver, DbContextResolver>();
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionScopeBehavior<,>));
        return builder;
    }

    public static IFlowCoreBuilder AddFlowCoreOutbox(this IFlowCoreBuilder builder)
    {
        builder.Services.AddHostedService<OutboxWorker>();
        return builder;
    }

    public static IFlowCoreBuilder AddFlowCoreDiagnostics(this IFlowCoreBuilder builder)
    {
        builder.Services.AddSingleton<IActivityFactory, SystemDiagnosticsActivityFactory>();
        builder.Services.AddSingleton<IMetricRecorder, SystemDiagnosticsMetricRecorder>();
        return builder;
    }

    public static IFlowCoreBuilder AddSaga<TSaga>(this IFlowCoreBuilder builder)
        where TSaga : Saga
    {
        builder.Services.AddScoped<TSaga>();
        return builder;
    }

    public static IFlowCoreBuilder AddFlowCoreSagaListener(this IFlowCoreBuilder builder)
    {
        builder.Services.AddScoped<IEventHandler<IEvent>, SagaEventListener>();
        return builder;
    }

    public static IFlowCoreBuilder AddFlowCoreScheduler(this IFlowCoreBuilder builder)
    {
        builder.Services.AddHostedService<SchedulerWorker>();
        return builder;
    }
}
