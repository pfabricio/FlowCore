using Microsoft.Extensions.DependencyInjection;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Execution;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace FlowCore;

public class FlowMediator : IFlowMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;

    public FlowMediator(IServiceProvider serviceProvider, IEventBus eventBus)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        return ExecutePipeline<ICommand<TResult>, TResult>(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(
        ICommand<Unit> command,
        CancellationToken cancellationToken = default)
    {
        return ExecutePipeline<ICommand<Unit>, Unit>(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResult> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        return ExecutePipeline<IQuery<TResult>, TResult>(query, cancellationToken);
    }

    private async Task<TResult> ExecutePipeline<TRequest, TResult>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        using var scope = new ExecutionScope(cancellationToken);

        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResult>>()
            .Reverse()
            .ToList();

        RequestHandlerDelegate<TResult> handler = () =>
        {
            return InvokeHandler<TRequest, TResult>(request, scope);
        };

        foreach (var behavior in behaviors)
        {
            var next = handler;
            handler = () => behavior.Handle(request, next, scope.CancellationToken);
        }

        return await handler();
    }

    private Task<TResult> InvokeHandler<TRequest, TResult>(
        TRequest request,
        ExecutionScope scope)
    {
        var requestType = request!.GetType();

        if (typeof(IQuery<TResult>).IsAssignableFrom(typeof(TRequest)))
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, typeof(TResult));
            var handler = _serviceProvider.GetRequiredService(handlerType);
            return InvokeHandleAsync<TResult>(handler, request, scope.CancellationToken);
        }

        var commandHandlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, typeof(TResult));
        var commandHandler = _serviceProvider.GetRequiredService(commandHandlerType);
        return InvokeHandleAsync<TResult>(commandHandler, request, scope.CancellationToken);
    }

    private static async Task<TResult> InvokeHandleAsync<TResult>(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var method = handler.GetType().GetMethod("HandleAsync")
            ?? throw new InvalidOperationException(
                $"Handler {handler.GetType().Name} does not have a HandleAsync method.");

        try
        {
            var task = (Task<TResult>)(method.Invoke(handler, new[] { request, cancellationToken })
                ?? throw new InvalidOperationException(
                    $"Handler {handler.GetType().Name}.HandleAsync returned null."));
            return await task;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <inheritdoc />
    public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        return _eventBus.PublishAsync(@event, cancellationToken);
    }
}
