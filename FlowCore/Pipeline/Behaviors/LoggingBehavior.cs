using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;
using System.Diagnostics;

namespace FlowCore.Pipeline.Behaviors;

public class LoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResult>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Handling {RequestName} - Type: {RequestType}", requestName, typeof(TRequest).FullName);
        else
            _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Handled {RequestName} in {ElapsedMilliseconds}ms - Type: {ResponseType}",
                requestName, stopwatch.ElapsedMilliseconds, typeof(TResult).FullName);
        else
            _logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}