using Pursuit.Application.DTOs;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Interfaces;

public interface IJobService
{
    Task<JobDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<JobDto>> SearchAsync(
        string? title,
        string? location,
        JobType? jobType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<JobDto> CreateAsync(CreateJobDto dto, CancellationToken cancellationToken = default);

    Task<JobDto> UpdateAsync(Guid id, UpdateJobDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobDto>> GetByTenantAsync(CancellationToken cancellationToken = default);
}