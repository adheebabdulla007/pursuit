using Pursuit.Application.DTOs;

namespace Pursuit.Application.Interfaces;

public interface IStatsService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}