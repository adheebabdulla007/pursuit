using Microsoft.AspNetCore.Http;
using Pursuit.Application.Interfaces;
using System.Security.Claims;

namespace Pursuit.Infrastructure.Services;

public class HttpDbContextScope : IDbContextScope
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpDbContextScope(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst("tenantId");

            if (claim is null) return null;

            return Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public Guid? CurrentUserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier);

            if (claim is null) return null;

            return Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}