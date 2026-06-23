# Logging — FlowCore

> How to implement logging and tracing in FlowCore.

---

## 📖 Overview

FlowCore supports logging through Pipeline Behaviors. This allows recording information about command and query execution for debugging and monitoring.

---

## 🎯 Creating a Logging Behavior

### Simple Behavior

```csharp
public class LoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
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

        _logger.LogInformation("Handling {RequestName}", requestName);

        var result = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);

        return result;
    }
}
```

### Behavior with Details

```csharp
public class DetailedLoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly ILogger<DetailedLoggingBehavior<TRequest, TResult>> _logger;

    public DetailedLoggingBehavior(ILogger<DetailedLoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("[{RequestId}] Processing {RequestName}", requestId, requestName);
        _logger.LogDebug("[{RequestId}] Request data: {@Request}", requestId, request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();

            stopwatch.Stop();
            _logger.LogInformation("[{RequestId}] Successfully processed {RequestName} in {ElapsedMs}ms",
                requestId, requestName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{RequestId}] Failed to process {RequestName} in {ElapsedMs}ms",
                requestId, requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## 📋 Configuring Logging

### In Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddLogging();

// Add logging behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

### Configuring the Logger

```csharp
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "FlowCore": "Debug"
    }
  }
}
```

---

## 🔧 Tracing with ActivitySource

### Creating an ActivitySource

```csharp
public static class FlowCoreActivitySource
{
    public static readonly ActivitySource Source = new("FlowCore");

    public static Activity? StartActivity(string name)
    {
        return Source.StartActivity(name);
    }
}
```

### Behavior with Tracing

```csharp
public class TracingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly ILogger<TracingBehavior<TRequest, TResult>> _logger;

    public TracingBehavior(ILogger<TracingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = FlowCoreActivitySource.StartActivity($"FlowCore.{requestName}");
        activity?.SetTag("request.type", requestName);

        try
        {
            var result = await next();
            activity?.SetTag("result.type", typeof(TResult).Name);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
```

---

## 🚀 Usage

### Automatic Logging

```csharp
// Logging is executed automatically for all commands and queries
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    // ...
}
```

### Structured Logging

```csharp
public class StructuredLoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly ILogger<StructuredLoggingBehavior<TRequest, TResult>> _logger;

    public StructuredLoggingBehavior(ILogger<StructuredLoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("RequestType", typeof(TRequest).Name))
        using (LogContext.PushProperty("RequestId", Guid.NewGuid()))
        {
            _logger.LogInformation("Processing request");

            var result = await next();

            _logger.LogInformation("Request completed");

            return result;
        }
    }
}
```

---

## 📝 Best Practices

1. **Use structured logging** - for easier searches and analysis
2. **Record execution time** - to identify bottlenecks
3. **Use appropriate levels** - Information for flow, Debug for details
4. **Handle errors gracefully** - log exceptions without failing the flow
5. **Consider performance** - logging should not significantly impact performance