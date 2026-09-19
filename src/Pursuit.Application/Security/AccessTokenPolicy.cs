using Microsoft.Extensions.Configuration;

namespace Pursuit.Application.Security;

public static class AccessTokenPolicy
{
    public const string SecurityVersionClaim = "security_version";
    public const int MaximumLifetimeMinutes = 15;

    public static int ReadLifetimeMinutes(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["JwtSettings:ExpiryInMinutes"], out var minutes)
            || minutes < 1 || minutes > MaximumLifetimeMinutes)
            throw new InvalidOperationException("JwtSettings:ExpiryInMinutes must be between 1 and 15.");
        return minutes;
    }
}
