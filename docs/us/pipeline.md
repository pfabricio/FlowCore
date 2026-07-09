# Pipeline — FlowCore

> How Pipeline Behaviors work in FlowCore.

---

## 📖 Overview

FlowCore's Pipeline allows intercepting commands and queries before, during, and after handler execution. This is useful for cross-cutting concerns like validation, logging, caching, and transactions.

> **New in v2.1.0**: An `ExecutionScope` with `IDiagnosticsContext` is automatically created per execution and available via `ExecutionScope.Current` in Behaviors and Handlers, without dependency injection.

---

## 🔧 Creating a Behavior

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
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);

        var result = await next();

        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);

        return result;
    }
}
```

### Behavior with Condition

```csharp
public class ValidationBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## 📋 Registering Behaviors

### In Program.cs

```csharp
builder.Services.AddFlowCore();

// Add behaviors in desired order
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

### Execution Order

```csharp
// Registration order determines execution order
// Validation → Logging → Caching → Handler

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

---

## 🎯 Practical Examples

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Caching Behavior

```csharp
public class CachingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly ICacheProvider _cacheProvider;

    public CachingBehavior(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var cachedResult = await _cacheProvider.GetAsync<TResult>(cacheableQuery.CacheKey, cancellationToken);
        if (cachedResult != null)
            return cachedResult;

        var result = await next();

        await _cacheProvider.SetAsync(cacheableQuery.CacheKey, result, cacheableQuery.Expiration, cancellationToken);

        return result;
    }
}
```

---

## 📝 Best Practices

1. **Order behaviors carefully** - validation first, logging after
2. **Keep behaviors lean** - each behavior should have a single responsibility
3. **Use generics** - to reuse across different request types
4. **Handle errors** - behaviors should handle exceptions gracefully
5. **Test in isolation** - each behavior should be tested independently