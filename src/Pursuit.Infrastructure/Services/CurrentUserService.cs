using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Enums;

namespace Pursuit.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier);

            if (claim is null)
                throw new InvalidOperationException("User is not authenticated.");

            return Guid.Parse(claim.Value);
        }
    }

    public Guid? TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst("tenantId");

            if (claim is null)
                return null;

            if (Guid.TryParse(claim.Value, out var tenantId))
                return tenantId;

            return null;
        }
    }

    public UserRole Role
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.Role);

            if (claim is null)
                throw new InvalidOperationException("User is not authenticated.");

            return Enum.Parse<UserRole>(claim.Value);
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor.HttpContext?
                .User
                .Identity?
                .IsAuthenticated ?? false;
        }
    }
}