using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public Task<bool> IsAuthenticationAllowedAsync(Guid userId, Guid securityVersion, UserRole role,
        Guid? tenantId, CancellationToken cancellationToken = default)
    {
        // Authentication precedes tenant context: this is a narrowly scoped identity
        // lookup, projecting only a boolean and bypassing ordinary tenant filters.
        return _dbSet.IgnoreQueryFilters().AsNoTracking().AnyAsync(user =>
            user.Id == userId && user.IsActive && user.SecurityVersion == securityVersion
            && user.Role == role && user.TenantId == tenantId
            && (user.TenantId == null || user.Tenant!.IsActive), cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Role == role, cancellationToken);
    }

    public async Task<User?> GetEmployerByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role == UserRole.Employer, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(u => u.Role == role)
            .CountAsync(cancellationToken);
    }

    public async Task<User?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is not null)
            await _context.Entry(user).ReloadAsync(cancellationToken);
        return user;
    }
}
