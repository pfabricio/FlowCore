# Cache — FlowCore

> Como implementar cache para queries no FlowCore.

---

## 📖 Visão Geral

O FlowCore suporta cache para queries através da interface `ICacheableQuery`. Isso permite armazenar resultados de queries frequentes e evitar consultas desnecessárias ao banco de dados.

---

## 🎯 Implementando Cache

### Query com Cache

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>, ICacheableQuery
{
    public string CacheKey => $"user:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

### Query com Cache Padrão

```csharp
public record GetActiveUsersQuery : IQuery<List<UserDto>>, ICacheableQuery
{
    public string CacheKey => "active_users";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

---

## 🔧 Implementando ICacheProvider

### Cache em Memória

```csharp
public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration;

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
```

### Cache com Redis

```csharp
public class RedisCacheProvider : ICacheProvider
{
    private readonly IDatabase _database;

    public RedisCacheProvider(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, serializedValue, expiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }
}
```

---

## 📋 Registrando o Cache

### No Program.cs

```csharp
// Cache em memória
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();

// Ou cache com Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<ICacheProvider, RedisCacheProvider>();
```

---

## 🚀 Uso

### Query com Cache

```csharp
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly MyDbContext _context;

    public GetUserByIdQueryHandler(MyDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // O cache é tratado automaticamente pelo CachingBehavior
        var user = await _context.Users.FindAsync(request.Id);
        return user == null ? throw new NotFoundException("User not found") : MapToDto(user);
    }
}
```

### Invalidação de Cache

```csharp
public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Unit>
{
    private readonly MyDbContext _context;
    private readonly ICacheProvider _cacheProvider;

    public UpdateUserCommandHandler(MyDbContext context, ICacheProvider cacheProvider)
    {
        _context = context;
        _cacheProvider = cacheProvider;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(request.Id);
        if (user == null)
            throw new NotFoundException("User not found");

        user.Name = request.Name;
        user.Email = request.Email;
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidar cache do usuário
        await _cacheProvider.RemoveAsync($"user:{request.Id}", cancellationToken);

        return Unit.Value;
    }
}
```

---

## 📝 Melhores Práticas

1. **Use chaves descritivas** - para facilitar debug e manutenção
2. **Defina expirações adequadas** - balance performance e consistência
3. **Invalidade cache quando necessário** - para manter dados atualizados
4. **Considere stale data** - cache pode retornar dados desatualizados
5. **Monitore hits e misses** - para otimizar estratégias de cache