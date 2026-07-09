using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using FlowCore.Core;
using FlowCore.Core.Interfaces;
using FlowCore.Execution;
using Microsoft.Extensions.DependencyInjection;

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

    public Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        return ExecutePipeline<ICommand<TResult>, TResult>(command, cancellationToken);
    }

    public Task SendAsync(
        ICommand<Unit> command,
        CancellationToken cancellationToken = default)
    {
        return ExecutePipeline<ICommand<Unit>, Unit>(command, cancellationToken);
    }

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
        if (TryDispatchWithGenerated<TResult>(handler, request, cancellationToken, out var result))
            return result;

        return await InvokeHandleWithReflectionAsync<TResult>(handler, request, cancellationToken);
    }

    private static bool TryDispatchWithGenerated<TResult>(
        object handler,
        object request,
        CancellationToken cancellationToken,
        out TResult result)
    {
        result = default!;

        try
        {
            var handleMethod = handler.GetType().GetMethod("HandleAsync",
                [request.GetType(), typeof(CancellationToken)]);

            if (handleMethod is null)
                return false;

            if (handleMethod.ReturnType.IsGenericType &&
                handleMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var task = (Task<TResult>)handleMethod.Invoke(handler, [request, cancellationToken])!;
                result = task.GetAwaiter().GetResult();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    [RequiresDynamicCode("The fallback handler invoker uses reflection. Use Source Generators for AOT compatibility.")]
    [RequiresUnreferencedCode("The fallback handler invoker uses reflection. Use Source Generators for AOT compatibility.")]
    private static async Task<TResult> InvokeHandleWithReflectionAsync<TResult>(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var method = handler.GetType().GetMethod("HandleAsync")
            ?? throw new InvalidOperationException(
                $"Handler {handler.GetType().Name} does not have a HandleAsync method.");

        try
        {
            var task = (Task<TResult>)(method.Invoke(handler, [request, cancellationToken])
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

    public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        return _eventBus.PublishAsync(@event, cancellationToken);
    }
}
