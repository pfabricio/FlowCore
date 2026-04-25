using Microsoft.Extensions.DependencyInjection;
using SuperMediaR.Core.Interfaces;
using System.Reflection;

namespace SuperMediaR;

public class SuperMediator : ISuperMediator
{
    private readonly IServiceProvider _serviceProvider;

    public SuperMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // -----------------------------
    // COMMAND
    // -----------------------------
    public async Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        return await ExecutePipeline<ICommand<TResult>, TResult>(command, cancellationToken);
    }

    // -----------------------------
    // QUERY
    // -----------------------------
    public async Task<TResult> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        return await ExecutePipeline<IQuery<TResult>, TResult>(query, cancellationToken);
    }

    // -----------------------------
    // PIPELINE EXECUTION
    // -----------------------------
    private Task<TResult> ExecutePipeline<TRequest, TResult>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        // Pega todos os behaviors dessa Request
        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResult>>()
            .Reverse()
            .ToList();

        // Handler final
        RequestHandlerDelegate<TResult> handler = async () =>
        {
            var handlerType = typeof(ICommandHandler<,>);
            if (typeof(IQuery<TResult>).IsAssignableFrom(typeof(TRequest)))
                handlerType = typeof(IQueryHandler<,>);

            var closed = handlerType.MakeGenericType(request.GetType(), typeof(TResult));

            dynamic instance = _serviceProvider.GetRequiredService(closed);

            return await instance.HandleAsync((dynamic)request, cancellationToken);
        };

        // Vai encaixando os behaviors em cadeia
        foreach (var behavior in behaviors)
        {
            var next = handler;
            handler = () => behavior.Handle(request, next, cancellationToken);
        }

        return handler();
    }

    // -----------------------------
    // EVENTS
    // -----------------------------
    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
            await ((dynamic)handler).HandleAsync((dynamic)@event, cancellationToken);
    }
}
