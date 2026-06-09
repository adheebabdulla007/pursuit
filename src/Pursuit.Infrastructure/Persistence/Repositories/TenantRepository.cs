using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(t => t.Slug == slug, cancellationToken);
    }
}