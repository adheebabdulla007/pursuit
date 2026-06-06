using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pursuit.Application.Interfaces;

namespace Pursuit.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var tenantIdClaim = _httpContextAccessor.HttpContext?
            .User
            .FindFirst("tenantId");

        if (tenantIdClaim is null)
            return null;

        if (Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            return tenantId;

        return null;
    }

    public bool HasTenant()
    {
        return GetTenantId() is not null;
    }
}