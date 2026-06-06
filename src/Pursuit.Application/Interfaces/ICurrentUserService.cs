using Pursuit.Domain.Enums;

namespace Pursuit.Application.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? TenantId { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}