# Transactions — FlowCore

> Como implementar transações com EF Core no FlowCore.

---

## 📖 Visão Geral

O FlowCore suporta transações através do Entity Framework Core. Isso permite garantir que múltiplas operações sejam executadas atomicamente.

---

## 🎯 Configurando Transações

### No Program.cs

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

## 🚀 Uso

### Command com Transação

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

### Múltiplas Operações

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

## 📝 Melhores Práticas

1. **Use transações apenas quando necessário** - nem toda operação precisa
2. **Mantenha transações curtas** - para reduzir bloqueios
3. **Trate erros graciosamente** - sempre faça rollback em caso de exceção
4. **Considere concorrência** - use optimistic concurrency quando aplicável
5. **Teste cenários de falha** - garanta que rollback funciona corretamente