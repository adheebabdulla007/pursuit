using Microsoft.Extensions.Configuration;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly int _refreshTokenExpiryInDays;

    public AuthService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenExpiryInDays = int.Parse(
            configuration["JwtSettings:RefreshTokenExpiryInDays"]
            ?? throw new InvalidOperationException("JwtSettings:RefreshTokenExpiryInDays is not configured."));
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role: {dto.Role}. Valid values are Employer, JobSeeker.");

        if (role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admin accounts cannot be created through registration.");

        if (await _userRepository.ExistsByEmailAsync(dto.Email, cancellationToken))
            throw new InvalidOperationException($"An account with email {dto.Email} already exists.");

        Guid? tenantId = null;

        if (role == UserRole.Employer)
        {
            if (string.IsNullOrWhiteSpace(dto.TenantName))
                throw new ArgumentException("Company name is required for employer registration.");

            var slug = GenerateSlug(dto.TenantName);

            if (await _tenantRepository.ExistsBySlugAsync(slug, cancellationToken))
                throw new InvalidOperationException($"A company with the name '{dto.TenantName}' already exists.");

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = dto.TenantName,
                Slug = slug,
                IsActive = true
            };

            await _tenantRepository.AddAsync(tenant, cancellationToken);
            tenantId = tenant.Id;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = role,
            TenantId = tenantId
        };

        await _userRepository.AddAsync(user, cancellationToken);

        var token = _tokenService.GenerateToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        var token = _tokenService.GenerateToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (existingToken.IsRevoked)
        {
            // Reuse of an already-rotated-away token — treat as a theft signal,
            // revoke every refresh token for this user, not just this one.
            await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId, cancellationToken);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existingToken.User.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existingToken.UserId,
            TokenHash = _tokenService.HashToken(newRawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryInDays),
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        existingToken.IsRevoked = true;
        existingToken.ReplacedByTokenId = newRefreshTokenEntity.Id;
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        var newAccessToken = _tokenService.GenerateToken(existingToken.User);

        return new AuthResponseDto
        {
            Token = newAccessToken,
            RefreshToken = newRawRefreshToken,
            Email = existingToken.User.Email,
            Role = existingToken.User.Role.ToString()
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
            return;

        existingToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);
    }

    private async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rawToken = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = _tokenService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryInDays),
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return rawToken;
    }

    private static string GenerateSlug(string name)
    {
        return name.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", string.Empty)
            .Replace(".", string.Empty)
            .Replace(",", string.Empty);
    }
}