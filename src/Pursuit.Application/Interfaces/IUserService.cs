using Pursuit.Application.DTOs;

namespace Pursuit.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetAllUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task SetUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
}