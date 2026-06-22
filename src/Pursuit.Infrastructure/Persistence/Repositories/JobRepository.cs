using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.Infrastructure.Persistence.Repositories;

public class JobRepository : Repository<Job>, IJobRepository
{
    public JobRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Job>> SearchAsync(
        string? title,
        string? location,
        JobType? jobType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(j => j.Title.Contains(title));

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(j => j.Location.Contains(location));

        if (jobType.HasValue)
            query = query.Where(j => j.JobType == jobType.Value);

        return await query
            .OrderBy(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? title,
        string? location,
        JobType? jobType,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(j => j.Title.Contains(title));

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(j => j.Location.Contains(location));

        if (jobType.HasValue)
            query = query.Where(j => j.JobType == jobType.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(j => j.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}