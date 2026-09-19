// IRefreshTokenRepository.cs
using Pursuit.Domain.Entities;

namespace Pursuit.Application.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    // Serializes refresh/logout operations for the token's user across API instances.
    // The callback's writes commit together; exceptions roll them back.
    Task<T> ExecuteWithTokenLockAsync<T>(
        string tokenHash,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
