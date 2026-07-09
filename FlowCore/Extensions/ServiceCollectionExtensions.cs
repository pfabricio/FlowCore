using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FlowCore;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Messaging;
using FlowCore.Pipeline.Behaviors;
using FlowCore.Saga;
using FlowCore.Scheduling;
using System.Reflection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowCore(this IServiceCollection services)
    {
        return services.AddFlowCore(AppDomain.CurrentDomain.GetAssemblies());
    }

    public static IServiceCollection AddFlowCore(this IServiceCollection services, params Assembly[] assemblies)
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

        return services;
    }

    /// <summary>
    /// Adiciona suporte a transações automáticas via EF Core.
    /// </summary>
    public static IServiceCollection AddFlowCoreTransactions(this IServiceCollection services)
    {
        services.AddScoped<IDbContextResolver, DbContextResolver>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionScopeBehavior<,>));
        return services;
    }

    /// <summary>
    /// Ativa o Outbox Worker para publicação confiável de eventos.
    /// </summary>
    public static IServiceCollection AddFlowCoreOutbox(this IServiceCollection services)
    {
        services.AddHostedService<OutboxWorker>();
        return services;
    }

    /// <summary>
    /// Ativa instrumentação com System.Diagnostics (Activity + Metrics).
    /// </summary>
    public static IServiceCollection AddFlowCoreDiagnostics(this IServiceCollection services)
    {
        services.AddSingleton<IActivityFactory, SystemDiagnosticsActivityFactory>();
        services.AddSingleton<IMetricRecorder, SystemDiagnosticsMetricRecorder>();
        return services;
    }

    /// <summary>
    /// Registra uma Saga e ativa o SagaCoordinator automaticamente.
    /// </summary>
    public static IServiceCollection AddSaga<TSaga>(this IServiceCollection services)
        where TSaga : Saga
    {
        services.AddScoped<TSaga>();
        return services;
    }

    /// <summary>
    /// Ativa o SagaEventListener para processamento automático de eventos de saga.
    /// </summary>
    public static IServiceCollection AddFlowCoreSagaListener(this IServiceCollection services)
    {
        services.AddScoped<IEventHandler<IEvent>, SagaEventListener>();
        return services;
    }

    /// <summary>
    /// Ativa o Scheduler Worker para publicação automática de mensagens agendadas.
    /// </summary>
    public static IServiceCollection AddFlowCoreScheduler(this IServiceCollection services)
    {
        services.AddHostedService<SchedulerWorker>();
        return services;
    }
}


