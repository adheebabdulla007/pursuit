// IRefreshTokenRepository.cs
using Pursuit.Domain.Entities;

namespace Pursuit.Application.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<T> ExecuteWithUserLockAsync<T>(Guid userId, Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

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

    Task RevokeTokenChainAsync(Guid userId, Guid tokenId,
        CancellationToken cancellationToken = default);
}
