# Logging — FlowCore

> Como implementar logging e tracing no FlowCore.

---

## 📖 Visão Geral

O FlowCore suporta logging através de Pipeline Behaviors. Isso permite registrar informações sobre a execução de commands e queries para debug e monitoramento.

---

## 🎯 Criando um Logging Behavior

### Behavior Simples

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

### Behavior com Detalhes

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

## 📋 Configurando o Logging

### No Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddLogging();

// Adicionar logging behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

### Configurando o Logger

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

## 🔧 Tracing com ActivitySource

### Criando um ActivitySource

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

### Behavior com Tracing

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

## 🚀 Uso

### Logging Automático

```csharp
// O logging é executado automaticamente para todos os commands e queries
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    // ...
}
```

### Logs Estruturados

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

## 📝 Melhores Práticas

1. **Use logging estruturado** - para facilitar buscas e análises
2. **Registre tempo de execução** - para identificar gargalos
3. **Use levels apropriados** - Information para fluxo, Debug para detalhes
4. **Trate erros graciosamente** - registre exceções sem falhar o fluxo
5. **Considere performance** - logging não deve impactar significativamente