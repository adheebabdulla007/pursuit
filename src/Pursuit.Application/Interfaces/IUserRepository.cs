using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetEmployerByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}