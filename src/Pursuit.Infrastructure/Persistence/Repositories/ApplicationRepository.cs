using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.Infrastructure.Persistence.Repositories;

public class ApplicationRepository : Repository<Domain.Entities.Application>, IApplicationRepository
{
    public ApplicationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Domain.Entities.Application>> GetByJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .Where(a => a.JobId == jobId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Application>> GetByApplicantAsync(
        Guid applicantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Applicant)
            .Include(a => a.Job)
            .Where(a => a.ApplicantId == applicantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid jobId,
        Guid applicantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(a => a.JobId == jobId && a.ApplicantId == applicantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Application>> GetByStatusAsync(
        ApplicationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Application?> GetByIdWithDetailsAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken);
    }
}