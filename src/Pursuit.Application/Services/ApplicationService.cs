using Microsoft.Extensions.Logging;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Application.Messages;
using Pursuit.Domain.Entities;

namespace Pursuit.Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IJobRepository jobRepository,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IMessagePublisher messagePublisher,
        ILogger<ApplicationService> logger)
    {
        _applicationRepository = applicationRepository;
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _messagePublisher = messagePublisher;
        _logger = logger;
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

        await PublishApplicationSubmittedAsync(application.Id, applicantId, job, cancellationToken);

        return MapToDto(application, job, null);
    }

    private async Task PublishApplicationSubmittedAsync(
        Guid applicationId,
        Guid applicantId,
        Job job,
        CancellationToken cancellationToken)
    {
        var applicant = await _userRepository.GetByIdAsync(applicantId, cancellationToken);
        var tenant = await _tenantRepository.GetByIdAsync(job.TenantId, cancellationToken);
        var employer = await _userRepository.GetEmployerByTenantIdAsync(job.TenantId, cancellationToken);

        if (applicant is null)
            _logger.LogWarning("Applicant {ApplicantId} not found when building ApplicationSubmittedMessage.", applicantId);

        if (tenant is null)
            _logger.LogWarning("Tenant {TenantId} not found when building ApplicationSubmittedMessage.", job.TenantId);

        if (employer is null)
            _logger.LogWarning("Employer for tenant {TenantId} not found when building ApplicationSubmittedMessage.", job.TenantId);

        var message = new ApplicationSubmittedMessage
        {
            ApplicationId = applicationId,
            ApplicantName = applicant is not null ? $"{applicant.FirstName} {applicant.LastName}" : string.Empty,
            ApplicantEmail = applicant?.Email ?? string.Empty,
            JobTitle = job.Title,
            CompanyName = tenant?.Name ?? string.Empty,
            EmployerEmail = employer?.Email ?? string.Empty
        };

        await _messagePublisher.PublishAsync(message, cancellationToken);
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

    public async Task<ApplicationDto> GetByIdAsync(
    Guid applicationId,
    CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application with ID {applicationId} was not found.");

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