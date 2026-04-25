using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SuperMediaR;
using SuperMediaR.Core.Interfaces;
using SuperMediaR.Pipeline.Behaviors;
using System.Reflection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Usa todos os assemblies carregados no AppDomain (comportamento atual).
    /// </summary>
    public static IServiceCollection AddSuperMediaR(this IServiceCollection services)
    {
        return services.AddSuperMediaR(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    /// Sobrecarga que permite registrar handlers somente dos assemblies informados.
    /// </summary>
    public static IServiceCollection AddSuperMediaR(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Core
        services.AddScoped<ISuperMediator, SuperMediator>();

        services.AddScoped<IDbContextResolver, DbContextResolver>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionScopeBehavior<,>));
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
}


