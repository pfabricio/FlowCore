using Microsoft.EntityFrameworkCore.Storage;
using SuperMediaR.Core.Interfaces;

namespace SuperMediaR.Pipeline.Behaviors;

public class TransactionScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IDbContextResolver _dbContextResolver;
    public TransactionScopeBehavior(IDbContextResolver dbContextResolver)
    {
        _dbContextResolver = dbContextResolver;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var dbContexts = _dbContextResolver.GetDbContexts().ToList();
        
        if (!dbContexts.Any() || dbContexts.Any(d => d.Database.CurrentTransaction != null))
            return await next();

        var transactions = new List<IDbContextTransaction>();

        try
        {
            foreach (var db in dbContexts)
                transactions.Add(await db.Database.BeginTransactionAsync(cancellationToken));

            var response = await next();

            foreach (var db in dbContexts)
                await db.SaveChangesAsync(cancellationToken);

            foreach (var t in transactions)
                await t.CommitAsync(cancellationToken);

            return response;
        }
        catch
        {
            foreach (var t in transactions)
                await t.RollbackAsync(cancellationToken);

            throw;
        }
    }
}