using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace FlowCore.Pipeline.Behaviors;

/// <summary>
/// Behavior que despacha automaticamente eventos de IEventSource após a execução do handler.
/// </summary>
/// <typeparam name="TRequest">Tipo do request.</typeparam>
/// <typeparam name="TResponse">Tipo do response.</typeparam>
public class EventDispatcherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatcherBehavior<TRequest, TResponse>> _logger;

    public EventDispatcherBehavior(IServiceProvider serviceProvider, ILogger<EventDispatcherBehavior<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        var events = ExtractEvents(request);

        if (!events.Any())
            return response;

        foreach (var @event in events)
        {
            await DispatchEvent(@event, cancellationToken);
        }

        return response;
    }

    private static IEnumerable<IEvent> ExtractEvents(TRequest request)
    {
        if (request is IEventSource eventSource && eventSource.Events.Any())
            return eventSource.Events;

        return Enumerable.Empty<IEvent>();
    }

    private async Task DispatchEvent(IEvent @event, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is null)
                {
                    _logger.LogError("Handler not registered for event: {EventType}", @event.GetType().Name);
                    throw new InvalidOperationException($"Event handler not registered for event type: {@event.GetType().Name}");
                }

                var method = handler.GetType().GetMethod("HandleAsync")
                    ?? throw new InvalidOperationException(
                        $"EventHandler {handler.GetType().Name} does not have a HandleAsync method.");

                await (Task)method.Invoke(handler, new object[] { @event, cancellationToken })!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                _logger.LogError(ex.InnerException, "Error handling event: {EventType}", @event.GetType().Name);
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event: {EventType}", @event.GetType().Name);
            }
        }
    }
}
