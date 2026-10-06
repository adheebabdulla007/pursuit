using Pursuit.Application.DTOs;

namespace Pursuit.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> ApplyAsync(
        CreateApplicationDto dto,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ApplicationDto>> GetMyApplicationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ApplicationDto>> GetByJobAsync(
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApplicationDto> UpdateStatusAsync(
        Guid applicationId,
        UpdateApplicationStatusDto dto,
        CancellationToken cancellationToken = default);

    Task<ApplicationDto> GetByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<string> GetResumeStorageUrlAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
