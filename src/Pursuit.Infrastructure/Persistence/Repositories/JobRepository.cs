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

    public new async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(j => j.Tenant)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> SearchAsync(
        string? title,
        string? location,
        JobType? jobType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(j => j.Tenant)
            .Where(j => j.IsActive);
        query = ApplySearchFilters(query, title, location, jobType);

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .ThenByDescending(j => j.Id)
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
        var query = _dbSet
            .Where(j => j.IsActive);
        query = ApplySearchFilters(query, title, location, jobType);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetByTenantAsync(
        Guid tenantId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(j => j.Tenant)
            .Where(j => j.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(j => j.IsActive == isActive.Value);

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .ThenByDescending(j => j.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByTenantAsync(
        Guid tenantId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(j => j.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(j => j.IsActive == isActive.Value);

        return await query.CountAsync(cancellationToken);
    }

    private static IQueryable<Job> ApplySearchFilters(
        IQueryable<Job> query,
        string? title,
        string? location,
        JobType? jobType)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            var titlePattern = ToLiteralSubstringPattern(title);
            query = query.Where(job => EF.Functions.ILike(job.Title, titlePattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationPattern = ToLiteralSubstringPattern(location);
            query = query.Where(job => EF.Functions.ILike(job.Location, locationPattern, "\\"));
        }

        if (jobType.HasValue)
            query = query.Where(job => job.JobType == jobType.Value);

        return query;
    }

    private static string ToLiteralSubstringPattern(string value) =>
        $"%{value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
