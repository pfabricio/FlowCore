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
    private readonly IHandlerResolver _handlerResolver;
    private readonly IEventBus _eventBus;
    private static readonly GeneratedDispatcherCache _generatedDispatcher = new();

    public FlowMediator(IServiceProvider serviceProvider, IHandlerResolver handlerResolver, IEventBus eventBus)
    {
        _serviceProvider = serviceProvider;
        _handlerResolver = handlerResolver;
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
        var previous = CurrentScope;
        var scope = new ExecutionScope(cancellationToken);
        CurrentScope = scope;
        try
        {
            var behaviors = _serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResult>>()
                .ToArray();

            RequestHandlerDelegate<TResult> handler = () =>
            {
                return InvokeHandler<TRequest, TResult>(request, scope);
            };

            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = handler;
                handler = () => behavior.Handle(request, next, scope.CancellationToken);
            }

            return await handler();
        }
        finally
        {
            scope.Dispose();
            CurrentScope = previous;
        }
    }

    private static ExecutionScope? CurrentScope
    {
        get => ExecutionScope.Current;
        set => ExecutionScope.Current = value;
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
        var handler = _handlerResolver.GetHandler(requestType, typeof(TResult));
        return await InvokeHandleWithReflectionAsync<TResult>(handler, request, scope.CancellationToken);
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
        private readonly object _syncLock = new();

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

            lock (_syncLock)
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

                    if (generatedType is null) return null;

                    var method = generatedType.GetMethod("DispatchAsync",
                        BindingFlags.Static | BindingFlags.Public,
                        [typeof(object), typeof(IServiceProvider), typeof(CancellationToken)]);

                    if (method is null) return null;

                    _dispatchFunc = (Func<object, IServiceProvider, CancellationToken, ValueTask<object?>>)
                        Delegate.CreateDelegate(
                            typeof(Func<object, IServiceProvider, CancellationToken, ValueTask<object?>>),
                            null,
                            method);
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
