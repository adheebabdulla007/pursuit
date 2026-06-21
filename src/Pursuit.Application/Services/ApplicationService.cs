using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;

namespace Pursuit.Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IJobRepository jobRepository,
        ICurrentUserService currentUserService)
    {
        _applicationRepository = applicationRepository;
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApplicationDto> ApplyAsync(
        CreateApplicationDto dto,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(dto.JobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job with ID {dto.JobId} was not found.");

        if (!job.IsActive)
            throw new InvalidOperationException("This job is no longer accepting applications.");

        var applicantId = _currentUserService.UserId;

        var alreadyApplied = await _applicationRepository.ExistsAsync(dto.JobId, applicantId, cancellationToken);

        if (alreadyApplied)
            throw new InvalidOperationException("You have already applied to this job.");

        var application = new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            JobId = dto.JobId,
            TenantId = job.TenantId,
            ApplicantId = applicantId,
            ResumeUrl = dto.ResumeUrl,
            Status = Domain.Enums.ApplicationStatus.Applied
        };

        await _applicationRepository.AddAsync(application, cancellationToken);

        return MapToDto(application, job, null);
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetMyApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        var applicantId = _currentUserService.UserId;

        var applications = await _applicationRepository.GetByApplicantAsync(applicantId, cancellationToken);

        return applications.Select(a => MapToDto(a, a.Job, a.Applicant)).ToList();
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetByJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var applications = await _applicationRepository.GetByJobAsync(jobId, cancellationToken);

        return applications.Select(a => MapToDto(a, a.Job, a.Applicant)).ToList();
    }

    public async Task<ApplicationDto> UpdateStatusAsync(
        Guid applicationId,
        UpdateApplicationStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId, cancellationToken)
                ?? throw new KeyNotFoundException($"Application with ID {applicationId} was not found.");

        application.Status = dto.Status;

        await _applicationRepository.UpdateAsync(application, cancellationToken);

        return MapToDto(application, application.Job, application.Applicant);
    }

    private static ApplicationDto MapToDto(Domain.Entities.Application application, Job? job, User? applicant) => new()
    {
        Id = application.Id,
        JobId = application.JobId,
        JobTitle = job?.Title ?? string.Empty,
        ApplicantId = application.ApplicantId,
        ApplicantName = applicant is not null ? $"{applicant.FirstName} {applicant.LastName}" : string.Empty,
        ResumeUrl = application.ResumeUrl,
        Status = application.Status,
        CreatedAt = application.CreatedAt
    };
}