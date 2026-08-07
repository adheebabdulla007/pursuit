using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;

namespace Pursuit.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UserService(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
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

    public async Task SetUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (userId == _currentUserService.UserId)
            throw new InvalidOperationException("You cannot change your own account status.");

        var user = await _userRepository.GetByIdIgnoringFiltersAsync(userId, cancellationToken) ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        user.IsActive = isActive;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        TenantId = user.TenantId,
        CreatedAt = user.CreatedAt,
        IsActive = user.IsActive
    };
}