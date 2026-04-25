using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SuperMediaR.Core.Interfaces;

namespace SuperMediaR.Pipeline.Behaviors;

public class DbContextResolver : IDbContextResolver
{
    private readonly IServiceProvider _provider;

    public DbContextResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IEnumerable<DbContext> GetDbContexts()
    {
        return _provider.GetServices<DbContext>();
    }
}