using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FlowCore;
using FlowCore.Core.Interfaces;
using FlowCore.Pipeline.Behaviors;
using System.Reflection;

/// <summary>
/// Extensões de configuração do FlowCore para IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra o FlowCore com todos os assemblies do AppDomain.
    /// Inclui: Logging, Validation, Caching e EventDispatcher behaviors.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>IServiceCollection para fluent API.</returns>
    public static IServiceCollection AddFlowCore(this IServiceCollection services)
    {
        return services.AddFlowCore(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    /// Registra o FlowCore com assemblies específicos.
    /// Inclui: Logging, Validation, Caching e EventDispatcher behaviors.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="assemblies">Assemblies para scan de handlers.</param>
    /// <returns>IServiceCollection para fluent API.</returns>
    public static IServiceCollection AddFlowCore(this IServiceCollection services, params Assembly[] assemblies)
    {
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
    /// Requer que o Microsoft.EntityFrameworkCore esteja disponível.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>IServiceCollection para fluent API.</returns>
    public static IServiceCollection AddFlowCoreTransactions(this IServiceCollection services)
    {
        services.AddScoped<IDbContextResolver, DbContextResolver>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionScopeBehavior<,>));
        return services;
    }
}


