using Pursuit.Application.DTOs;

namespace Pursuit.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> ApplyAsync(
        CreateApplicationDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationDto>> GetMyApplicationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationDto>> GetByJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<ApplicationDto> UpdateStatusAsync(
        Guid applicationId,
        UpdateApplicationStatusDto dto,
        CancellationToken cancellationToken = default);
}