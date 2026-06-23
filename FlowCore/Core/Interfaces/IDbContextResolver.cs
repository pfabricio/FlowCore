using Microsoft.EntityFrameworkCore;

namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para resolução de DbContexts registrados no DI.
/// Usado pelo TransactionScopeBehavior para gerenciar transações.
/// </summary>
public interface IDbContextResolver
{
    /// <summary>
    /// Obtém todos os DbContexts registrados no container de DI.
    /// </summary>
    /// <returns>Coleção de DbContexts.</returns>
    IEnumerable<DbContext> GetDbContexts();
}
