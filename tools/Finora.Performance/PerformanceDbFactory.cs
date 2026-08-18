using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.Performance;

internal sealed class PerformanceDbFactory(DbContextOptions<FinoraDbContext> options) : IDbContextFactory<FinoraDbContext>
{
    private readonly DbContextOptions<FinoraDbContext> _options = options;

    public FinoraDbContext CreateDbContext() => new(_options);

    public Task<FinoraDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }
}