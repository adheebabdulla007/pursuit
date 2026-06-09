using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
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

        return new AuthResponseDto
        {
            Token = token,
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

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString()
        };
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