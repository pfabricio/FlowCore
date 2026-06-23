# Transactions — FlowCore

> How to implement transactions with EF Core in FlowCore.

---

## 📖 Overview

FlowCore supports transactions through Entity Framework Core. This ensures multiple operations are executed atomically.

---

## 🎯 Configuring Transactions

### In Program.cs

```csharp
builder.Services.AddFlowCore();
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Transaction Behavior

```csharp
public class TransactionBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly MyDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResult>> _logger;

    public TransactionBehavior(MyDbContext dbContext, ILogger<TransactionBehavior<TRequest, TResult>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction != null)
            return await next();

        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[TRANSACTION] Beginning transaction for {RequestName}", requestName);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await next();
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("[TRANSACTION] Transaction committed for {RequestName}", requestName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TRANSACTION] Transaction rolled back for {RequestName}", requestName);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

## 🚀 Usage

### Command with Transaction

```csharp
public record CreateOrderCommand(Guid UserId, List<OrderItemDto> Items) : ICommand<Guid>;

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly MyDbContext _context;

    public CreateOrderCommandHandler(MyDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
```

### Multiple Operations

```csharp
public record TransferMoneyCommand(Guid FromAccountId, Guid ToAccountId, decimal Amount) : ICommand<Unit>;

public class TransferMoneyCommandHandler : ICommandHandler<TransferMoneyCommand, Unit>
{
    private readonly MyDbContext _context;

    public TransferMoneyCommandHandler(MyDbContext context) => _context = context;

    public async Task<Unit> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {
        var fromAccount = await _context.Accounts.FindAsync(request.FromAccountId);
        var toAccount = await _context.Accounts.FindAsync(request.ToAccountId);

        if (fromAccount == null || toAccount == null)
            throw new NotFoundException("Account not found");

        if (fromAccount.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient funds");

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

---

## 📝 Best Practices

1. **Use transactions only when needed** - not every operation requires them
2. **Keep transactions short** - to reduce locking
3. **Handle errors gracefully** - always rollback on exception
4. **Consider concurrency** - use optimistic concurrency when applicable
5. **Test failure scenarios** - ensure rollback works correctly