using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Services;

public class StatsService : IStatsService
{
    private readonly IUserRepository _userRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IApplicationRepository _applicationRepository;

    public StatsService(
        IUserRepository userRepository,
        IJobRepository jobRepository,
        IApplicationRepository applicationRepository)
    {
        _userRepository = userRepository;
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _userRepository.CountAsync(cancellationToken);
        var totalEmployers = await _userRepository.CountByRoleAsync(UserRole.Employer, cancellationToken);
        var totalJobSeekers = await _userRepository.CountByRoleAsync(UserRole.JobSeeker, cancellationToken);
        var totalJobs = await _jobRepository.CountAsync(null, null, null, cancellationToken);
        var totalApplications = await _applicationRepository.CountAsync(cancellationToken);

        return new AdminStatsDto
        {
            TotalUsers = totalUsers,
            TotalEmployers = totalEmployers,
            TotalJobSeekers = totalJobSeekers,
            TotalJobs = totalJobs,
            TotalApplications = totalApplications
        };
    }
}