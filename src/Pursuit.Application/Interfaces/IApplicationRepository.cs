using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Interfaces;

public interface IApplicationRepository : IRepository<Domain.Entities.Application>
{
    Task<IReadOnlyList<Domain.Entities.Application>> GetByJobAsync(
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Entities.Application>> GetByApplicantAsync(
        Guid applicantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByApplicantAsync(Guid applicantId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid jobId,
        Guid applicantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Entities.Application>> GetByStatusAsync(
        ApplicationStatus status,
        CancellationToken cancellationToken = default);

    Task<Domain.Entities.Application?> GetByIdWithDetailsAsync(
    Guid id,
    CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
