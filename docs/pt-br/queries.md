# Queries — FlowCore

> Como criar e utilizar consultas no FlowCore.

---

## 📖 Visão Geral

Consultas representam operações de leitura que não alteram o estado do sistema. No FlowCore, consultas implementam a interface `IQuery<TResult>`.

---

## 🎯 Criando uma Query

### Query Simples

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;
```

### Query com Parâmetros

```csharp
public record GetUsersQuery(string? Search, int Page, int PageSize) : IQuery<PagedResult<UserDto>>;
```

---

## 🔧 Criando um Handler

### Handler Simples

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

### Handler com Paginação

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

## 🚀 Uso

### Injeção de Dependência

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

### Implementando ICacheableQuery

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>, ICacheableQuery
{
    public string CacheKey => $"user:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

---

## 📝 Melhores Práticas

1. **Use records** para queries - são imutáveis e possuem igualdade por valor
2. **Mantenha queries read-only** - não altere o estado do sistema
3. **Use projeção** - selecione apenas os campos necessários
4. **Implemente paginação** - para listas grandes de dados
5. **Use cache** - para queries frequentes que não mudam souventemente