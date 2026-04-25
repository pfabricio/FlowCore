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

        _logger.LogInformation("📥 Handling {RequestName} - Payload: {@Request}", requestName, request);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation("📤 Handled {RequestName} in {ElapsedMilliseconds}ms - Response: {@Response}",
            requestName, stopwatch.ElapsedMilliseconds, response);

        return response;
    }
}