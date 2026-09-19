using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<T> ExecuteWithTokenLockAsync<T>(
        string tokenHash,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        // Resolve the immutable owner before locking. All tokens belonging to that
        // user share one lock, including an ancestor being replayed during rotation.
        var userId = await _dbSet.AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        return await ExecuteWithUserLockAsync(userId ?? Guid.Empty, operation, cancellationToken);
    }

    public async Task<T> ExecuteWithUserLockAsync<T>(Guid userId, Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT [Id] FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}",
            cancellationToken);

        var result = await operation();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbSet
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
        if (token is not null)
        {
            // A reused scope may already track snapshots from before the lock.
            await _context.Entry(token).ReloadAsync(cancellationToken);
            await _context.Entry(token.User).ReloadAsync(cancellationToken);
        }
        return token;
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeTokenChainAsync(Guid userId, Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        // Traverse only indexed replacement links. This keeps work proportional
        // to one device's rotation chain, regardless of other sessions or age.
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            ;WITH TokenChain AS (
                SELECT [Id], [ReplacedByTokenId] FROM [RefreshTokens]
                WHERE [Id] = {tokenId} AND [UserId] = {userId}
                UNION ALL
                SELECT child.[Id], child.[ReplacedByTokenId]
                FROM [RefreshTokens] AS child
                INNER JOIN TokenChain AS parent ON child.[Id] = parent.[ReplacedByTokenId]
                WHERE child.[UserId] = {userId}
            )
            UPDATE token SET [IsRevoked] = 1, [UpdatedAt] = SYSUTCDATETIME()
            FROM [RefreshTokens] AS token
            INNER JOIN TokenChain AS chain ON chain.[Id] = token.[Id]
            OPTION (MAXRECURSION 32767)
            """, cancellationToken);
    }
}
