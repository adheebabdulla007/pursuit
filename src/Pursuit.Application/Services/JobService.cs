using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;

    public JobService(IJobRepository jobRepository, ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
    }

    public async Task<JobDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);

        if (job is null)
            throw new KeyNotFoundException($"Job with ID {id} was not found.");

        return MapToDto(job);
    }

    public async Task<PagedResult<JobDto>> SearchAsync(
        string? title,
        string? location,
        JobType? jobType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _jobRepository.SearchAsync(title, location, jobType, page, pageSize, cancellationToken);
        var total = await _jobRepository.CountAsync(title, location, jobType, cancellationToken);

        return new PagedResult<JobDto>
        {
            Items = jobs.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<JobDto> CreateAsync(CreateJobDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new UnauthorizedAccessException("Employer tenant not found.");

        var job = new Job
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            SalaryMin = dto.SalaryMin,
            SalaryMax = dto.SalaryMax,
            JobType = dto.JobType,
            IsActive = true
        };

        await _jobRepository.AddAsync(job, cancellationToken);

        return MapToDto(job);
    }

    public async Task<JobDto> UpdateAsync(Guid id, UpdateJobDto dto, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job with ID {id} was not found.");

        job.Title = dto.Title;
        job.Description = dto.Description;
        job.Location = dto.Location;
        job.SalaryMin = dto.SalaryMin;
        job.SalaryMax = dto.SalaryMax;
        job.JobType = dto.JobType;
        job.IsActive = dto.IsActive;

        await _jobRepository.UpdateAsync(job, cancellationToken);

        return MapToDto(job);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job with ID {id} was not found.");

        await _jobRepository.DeleteAsync(job, cancellationToken);
    }

    public async Task<IReadOnlyList<JobDto>> GetByTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new UnauthorizedAccessException("Employer tenant not found.");

        var jobs = await _jobRepository.GetByTenantAsync(tenantId, cancellationToken);

        return jobs.Select(MapToDto).ToList();
    }

    private static JobDto MapToDto(Job job) => new()
    {
        Id = job.Id,
        TenantId = job.TenantId,
        Title = job.Title,
        Description = job.Description,
        Location = job.Location,
        SalaryMin = job.SalaryMin,
        SalaryMax = job.SalaryMax,
        JobType = job.JobType,
        IsActive = job.IsActive,
        CreatedAt = job.CreatedAt
    };
}