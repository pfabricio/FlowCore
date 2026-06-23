# Pipeline — FlowCore

> Como funcionam os Pipeline Behaviors no FlowCore.

---

## 📖 Visão Geral

O Pipeline do FlowCore permite interceptar commands e queries antes, durante e após a execução do handler. Isso é útil para cross-cutting concerns como validação, logging, cache e transações.

---

## 🔧 Criando um Behavior

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
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);

        var result = await next();

        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);

        return result;
    }
}
```

### Behavior com Condição

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

## 📋 Registrando Behaviors

### No Program.cs

```csharp
builder.Services.AddFlowCore();

// Adicionar behaviors na ordem desejada
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

### Ordem de Execução

```csharp
// A ordem de registro determina a ordem de execução
// Validation → Logging → Caching → Handler

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

---

## 🎯 Exemplos Práticos

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

## 📝 Melhores Práticas

1. **Ordene behaviors cuidadosamente** - validação primeiro, logging depois
2. **Mantenha behaviors enxutos** - cada behavior deve ter uma responsabilidade
3. **Use genéricos** - para reutilizar em diferentes tipos de request
4. **Trate erros** - behaviors devem lidar com exceções graciosamente
5. **Teste isoladamente** - cada behavior deve ser testado independentemente