using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;

namespace FlowCore.Pipeline.Behaviors;

public class EventDispatcherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventDispatcherBehavior<TRequest, TResponse>> _logger;

    public EventDispatcherBehavior(IEventBus eventBus, ILogger<EventDispatcherBehavior<TRequest, TResponse>> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IEventSource eventSource && eventSource.Events.Any())
        {
            foreach (var @event in eventSource.Events)
            {
                try
                {
                    await _eventBus.PublishAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching event: {EventType}", @event.GetType().Name);
                    throw;
                }
            }
        }

        return response;
    }
}
