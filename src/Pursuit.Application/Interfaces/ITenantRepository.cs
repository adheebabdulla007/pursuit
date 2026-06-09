using Pursuit.Domain.Entities;

namespace Pursuit.Application.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
}