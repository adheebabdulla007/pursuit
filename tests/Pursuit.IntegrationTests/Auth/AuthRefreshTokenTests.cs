using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.IntegrationTests.Auth;

[Collection("Integration Tests")]
public class AuthRefreshTokenTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthRefreshTokenTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RefreshTokenAsync_ConcurrentRequests_OnlyOneRotatesAndReplayRevokesReplacement()
    {
        using var setup = _factory.Services.CreateScope();
        var initial = await setup.ServiceProvider.GetRequiredService<IAuthService>().RegisterAsync(new RegisterDto
        {
            FirstName = "Concurrent", LastName = "Refresh",
            Email = $"concurrent-{Guid.NewGuid()}@test.com",
            Password = "TestPassword123!", Role = "JobSeeker"
        });
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyCount = 0;
        var requests = Enumerable.Range(0, 8).Select(async _ =>
        {
            using var scope = _factory.Services.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var hash = scope.ServiceProvider.GetRequiredService<ITokenService>().HashToken(initial.RefreshToken);
            // Deliberately preload the same valid snapshot in every context. The
            // transaction must re-read it after acquiring the database lock.
            await scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>().GetByTokenHashAsync(hash);
            if (Interlocked.Increment(ref readyCount) == 8)
                ready.SetResult();
            await start.Task;
            try { return await auth.RefreshTokenAsync(initial.RefreshToken); }
            catch (UnauthorizedAccessException) { return null; }
        }).ToArray();
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        start.SetResult();
        var results = await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(30));
        results.Should().ContainSingle(result => result != null);

        using var verification = _factory.Services.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Email == initial.Email);
        var tokens = await db.RefreshTokens.Where(token => token.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(2, "a refresh token must have only one successor");
        tokens.Should().OnlyContain(token => token.IsRevoked);
        var winner = results.Single(result => result != null)!;
        var replay = () => verification.ServiceProvider.GetRequiredService<IAuthService>()
            .RefreshTokenAsync(winner.RefreshToken);
        await replay.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task TokenTransaction_FailureAfterInsert_RollsBackReplacementAndOldTokenUpdate()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var initial = await services.GetRequiredService<IAuthService>().RegisterAsync(new RegisterDto
        {
            FirstName = "Rollback", LastName = "Refresh",
            Email = $"rollback-{Guid.NewGuid()}@test.com",
            Password = "TestPassword123!", Role = "JobSeeker"
        });
        var repository = services.GetRequiredService<IRefreshTokenRepository>();
        var hash = services.GetRequiredService<ITokenService>().HashToken(initial.RefreshToken);
        var replacementId = Guid.NewGuid();
        var fail = () => repository.ExecuteWithTokenLockAsync<bool>(hash, async () =>
        {
            var original = (await repository.GetByTokenHashAsync(hash))!;
            await repository.AddAsync(new RefreshToken
            {
                Id = replacementId, UserId = original.UserId,
                TokenHash = new string('a', 32) + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            original.IsRevoked = true;
            original.ReplacedByTokenId = replacementId;
            await repository.UpdateAsync(original);
            throw new InvalidOperationException("Injected transaction failure");
        });
        await fail.Should().ThrowAsync<InvalidOperationException>();

        using var verification = _factory.Services.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.RefreshTokens.AnyAsync(token => token.Id == replacementId)).Should().BeFalse();
        var original = await db.RefreshTokens.SingleAsync(token => token.TokenHash == hash);
        original.IsRevoked.Should().BeFalse();
        original.ReplacedByTokenId.Should().BeNull();
        var retry = await verification.ServiceProvider.GetRequiredService<IAuthService>()
            .RefreshTokenAsync(initial.RefreshToken);
        retry.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokensAndMarksOldTokenAsRevokedAndReplaced()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var email = $"refresh-happy-{Guid.NewGuid()}@test.com";
        var registerDto = new RegisterDto
        {
            FirstName = "Happy",
            LastName = "User",
            Email = email,
            Password = "TestPassword123!",
            Role = "JobSeeker"
        };

        var initialResponse = await authService.RegisterAsync(registerDto);

        // Act
        var refreshedResponse = await authService.RefreshTokenAsync(initialResponse.RefreshToken);

        // Assert
        refreshedResponse.Should().NotBeNull();
        refreshedResponse.Token.Should().NotBeNullOrEmpty();
        refreshedResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshedResponse.RefreshToken.Should().NotBe(initialResponse.RefreshToken);
        refreshedResponse.Email.Should().Be(email);
        refreshedResponse.Role.Should().Be("JobSeeker");

        // Verify JTI in new access token differs from old access token
        var jwtHandler = new JwtSecurityTokenHandler();
        var oldJwt = jwtHandler.ReadJwtToken(initialResponse.Token);
        var newJwt = jwtHandler.ReadJwtToken(refreshedResponse.Token);
        newJwt.Id.Should().NotBeNullOrEmpty();
        newJwt.Id.Should().NotBe(oldJwt.Id);

        // Verify token hashes differ
        var oldHash = tokenService.HashToken(initialResponse.RefreshToken);
        var newHash = tokenService.HashToken(refreshedResponse.RefreshToken);
        newHash.Should().NotBe(oldHash);

        // Verify DB updates
        var oldTokenEntity = await db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(rt => rt.TokenHash == oldHash);
        var newTokenEntity = await db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(rt => rt.TokenHash == newHash);

        oldTokenEntity.Should().NotBeNull();
        newTokenEntity.Should().NotBeNull();

        oldTokenEntity!.IsRevoked.Should().BeTrue();
        oldTokenEntity.ReplacedByTokenId.Should().Be(newTokenEntity!.Id);

        newTokenEntity.IsRevoked.Should().BeFalse();
        newTokenEntity.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenNotFound_ThrowsUnauthorizedAccessExceptionWithExactMessage()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var nonExistentToken = $"invalid-token-{Guid.NewGuid()}";

        // Act
        var act = () => authService.RefreshTokenAsync(nonExistentToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ReuseAfterRotation_ThrowsInvalidRefreshTokenAndRevokesEntireTokenChain()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"reuse-theft-{Guid.NewGuid()}@test.com";
        var initialResponse = await authService.RegisterAsync(new RegisterDto
        {
            FirstName = "Theft",
            LastName = "Test",
            Email = email,
            Password = "TestPassword123!",
            Role = "JobSeeker"
        });

        // First rotation (valid)
        var firstRotation = await authService.RefreshTokenAsync(initialResponse.RefreshToken);

        // Second rotation (valid)
        var secondRotation = await authService.RefreshTokenAsync(firstRotation.RefreshToken);

        // Get user ID
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == email);

        // Verify prior state: secondRotation token is not revoked yet
        var userTokensBeforeTheft = await db.RefreshTokens.IgnoreQueryFilters().Where(rt => rt.UserId == user.Id).ToListAsync();
        userTokensBeforeTheft.Should().ContainSingle(rt => !rt.IsRevoked);

        // Act: Reuse initial token that was already rotated away
        var act = () => authService.RefreshTokenAsync(initialResponse.RefreshToken);

        // Assert: Throws exact exception message
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");

        // Assert: Every refresh token for this user must now be revoked
        var userTokensAfterTheft = await db.RefreshTokens.IgnoreQueryFilters().Where(rt => rt.UserId == user.Id).ToListAsync();
        userTokensAfterTheft.Should().NotBeEmpty();
        userTokensAfterTheft.Should().AllSatisfy(rt => rt.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task RefreshTokenAsync_EmployerRefreshToken_SuccessfullyRotatesToken()
    {
        // Arrange: Regression test for tenant filter bug where .Include(rt => rt.User) silently excluded Employers
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"employer-refresh-{Guid.NewGuid()}@test.com";
        var employerResponse = await authService.RegisterAsync(new RegisterDto
        {
            FirstName = "Employer",
            LastName = "Owner",
            Email = email,
            Password = "TestPassword123!",
            Role = "Employer",
            TenantName = $"Tenant {Guid.NewGuid()}"
        });

        // Act
        var refreshedResponse = await authService.RefreshTokenAsync(employerResponse.RefreshToken);

        // Assert
        refreshedResponse.Should().NotBeNull();
        refreshedResponse.Token.Should().NotBeNullOrEmpty();
        refreshedResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshedResponse.RefreshToken.Should().NotBe(employerResponse.RefreshToken);
        refreshedResponse.Email.Should().Be(email);
        refreshedResponse.Role.Should().Be("Employer");

        var employer = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
        employer.TenantId.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var email = $"expired-token-{Guid.NewGuid()}@test.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            FirstName = "Expired",
            LastName = "Token",
            Role = UserRole.JobSeeker,
            IsActive = true
        };
        db.Users.Add(user);

        var expiredRawToken = tokenService.GenerateRefreshToken();
        var expiredRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashToken(expiredRawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };
        db.RefreshTokens.Add(expiredRefreshTokenEntity);
        await db.SaveChangesAsync();

        // Act
        var act = () => authService.RefreshTokenAsync(expiredRawToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshTokenAsync_DeactivatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"deactivated-{Guid.NewGuid()}@test.com";
        var registerResponse = await authService.RegisterAsync(new RegisterDto
        {
            FirstName = "Deactivated",
            LastName = "User",
            Email = email,
            Password = "TestPassword123!",
            Role = "JobSeeker"
        });

        var user = await db.Users.FirstAsync(u => u.Email == email);
        user.IsActive = false;
        db.Users.Update(user);
        await db.SaveChangesAsync();

        // Act
        var act = () => authService.RefreshTokenAsync(registerResponse.RefreshToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("This account has been deactivated.");
    }
}
