using Microsoft.EntityFrameworkCore;

namespace FlowCore.Core.Interfaces;

public interface IDbContextResolver
{
    IEnumerable<DbContext> GetDbContexts();
}