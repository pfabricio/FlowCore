# Queries — FlowCore

> How to create and use queries in FlowCore.

---

## 📖 Overview

Queries represent read operations that do not change the system state. In FlowCore, queries implement the `IQuery<TResult>` interface.

---

## 🎯 Creating a Query

### Simple Query

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;
```

### Query with Parameters

```csharp
public record GetUsersQuery(string? Search, int Page, int PageSize) : IQuery<PagedResult<UserDto>>;
```

---

## 🔧 Creating a Handler

### Simple Handler

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
        var user = await _context.Users
            .Where(u => u.Id == request.Id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            throw new NotFoundException("User not found");

        return user;
    }
}
```

### Handler with Pagination

```csharp
public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly MyDbContext _context;

    public GetUsersQueryHandler(MyDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(u => u.Name.Contains(request.Search) || u.Email.Contains(request.Search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
```

---

## 🚀 Usage

### Dependency Injection

```csharp
public class UserService
{
    private readonly IFlowMediator _mediator;

    public UserService(IFlowMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<UserDto> GetUserAsync(Guid id)
    {
        return await _mediator.QueryAsync(new GetUserByIdQuery(id));
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(string? search, int page, int pageSize)
    {
        return await _mediator.QueryAsync(new GetUsersQuery(search, page, pageSize));
    }
}
```

---

## 🔐 Cache

### Implementing ICacheableQuery

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>, ICacheableQuery
{
    public string CacheKey => $"user:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

---

## 📝 Best Practices

1. **Use records** for queries - they are immutable and have value equality
2. **Keep queries read-only** - do not change the system state
3. **Use projection** - select only the necessary fields
4. **Implement pagination** - for large data lists
5. **Use cache** - for frequent queries that don't change often