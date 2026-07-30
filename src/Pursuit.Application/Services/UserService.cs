using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;

namespace Pursuit.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserDto>> GetAllUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var total = await _userRepository.CountAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        TenantId = user.TenantId,
        CreatedAt = user.CreatedAt
    };
}