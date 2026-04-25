using Microsoft.EntityFrameworkCore;

namespace SuperMediaR.Core.Interfaces;

public interface IDbContextResolver
{
    IEnumerable<DbContext> GetDbContexts();
}