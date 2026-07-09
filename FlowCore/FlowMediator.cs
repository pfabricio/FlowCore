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
    private static readonly GeneratedDispatcherCache _generatedDispatcher = new();

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

    private async Task<TResult> InvokeHandler<TRequest, TResult>(
        TRequest request,
        ExecutionScope scope)
    {
        var generatedResult = await _generatedDispatcher.TryDispatchAsync<TResult>(
            request!, _serviceProvider, scope.CancellationToken);
        if (generatedResult.IsSuccess)
            return generatedResult.Value;

        var requestType = request!.GetType();

        if (typeof(IQuery<TResult>).IsAssignableFrom(typeof(TRequest)))
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, typeof(TResult));
            var handler = _serviceProvider.GetRequiredService(handlerType);
            return await InvokeHandleWithReflectionAsync<TResult>(handler, request, scope.CancellationToken);
        }

        var commandHandlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, typeof(TResult));
        var commandHandler = _serviceProvider.GetRequiredService(commandHandlerType);
        return await InvokeHandleWithReflectionAsync<TResult>(commandHandler, request, scope.CancellationToken);
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

    private sealed class GeneratedDispatcherCache
    {
        private Func<object, IServiceProvider, CancellationToken, ValueTask<object?>>? _dispatchFunc;
        private bool _initialized;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "FlowCore.Generated.GeneratedDispatcher", "FlowCore")]
        public async ValueTask<DispatchResult<TResult>> TryDispatchAsync<TResult>(
            object request,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var func = LazyInitialize();
            if (func is null)
                return DispatchResult<TResult>.NotHandled;

            try
            {
                var result = await func(request, services, cancellationToken);
                return DispatchResult<TResult>.Success((TResult)result!);
            }
            catch (InvalidOperationException)
            {
                return DispatchResult<TResult>.NotHandled;
            }
        }

        private Func<object, IServiceProvider, CancellationToken, ValueTask<object?>>? LazyInitialize()
        {
            if (_initialized)
                return _dispatchFunc;

            lock (this)
            {
                if (_initialized)
                    return _dispatchFunc;

                _initialized = true;

                try
                {
                    var generatedType = Type.GetType(
                        "FlowCore.Generated.GeneratedDispatcher, FlowCore",
                        throwOnError: false);

                    if (generatedType is null)
                    {
                        generatedType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t =>
                                t.FullName == "FlowCore.Generated.GeneratedDispatcher");
                    }

                    if (generatedType is null)
                        return null;

                    var method = generatedType.GetMethod("DispatchAsync",
                        BindingFlags.Static | BindingFlags.Public,
                        [typeof(object), typeof(IServiceProvider), typeof(CancellationToken)]);

                    if (method is null)
                        return null;

                    _dispatchFunc = (obj, sp, ct) =>
                    {
                        var task = (ValueTask<object?>)method.Invoke(null, [obj, sp, ct])!;
                        return task;
                    };
                }
                catch
                {
                }

                return _dispatchFunc;
            }
        }
    }

    private readonly struct DispatchResult<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }

        private DispatchResult(bool isSuccess, T value)
        {
            IsSuccess = isSuccess;
            Value = value;
        }

        public static DispatchResult<T> Success(T value) => new(true, value);
        public static DispatchResult<T> NotHandled => new(false, default!);
    }
}
