# Cache — FlowCore

> How to implement cache for queries in FlowCore.

---

## 📖 Overview

FlowCore supports query caching through the `ICacheableQuery` interface. This allows storing results of frequent queries and avoiding unnecessary database queries.

---

## 🎯 Implementing Cache

### Query with Cache

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>, ICacheableQuery
{
    public string CacheKey => $"user:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

### Query with Default Cache

```csharp
public record GetActiveUsersQuery : IQuery<List<UserDto>>, ICacheableQuery
{
    public string CacheKey => "active_users";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

---

## 🔧 Implementing ICacheProvider

### In-Memory Cache

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

### Redis Cache

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

## 📋 Registering Cache

### In Program.cs

```csharp
// In-memory cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();

// Or Redis cache
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<ICacheProvider, RedisCacheProvider>();
```

---

## 🚀 Usage

### Query with Cache

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
        // Cache is handled automatically by CachingBehavior
        var user = await _context.Users.FindAsync(request.Id);
        return user == null ? throw new NotFoundException("User not found") : MapToDto(user);
    }
}
```

### Cache Invalidation

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

        // Invalidate user cache
        await _cacheProvider.RemoveAsync($"user:{request.Id}", cancellationToken);

        return Unit.Value;
    }
}
```

---

## 📝 Best Practices

1. **Use descriptive keys** - for easier debugging and maintenance
2. **Set appropriate expirations** - balance performance and consistency
3. **Invalidate cache when needed** - to keep data up to date
4. **Consider stale data** - cache may return outdated data
5. **Monitor hits and misses** - to optimize cache strategies