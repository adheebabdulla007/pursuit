using Pursuit.Domain.Entities;

namespace Pursuit.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetEmployerByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}