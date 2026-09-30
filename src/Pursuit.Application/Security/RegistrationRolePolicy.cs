using Pursuit.Domain.Enums;

namespace Pursuit.Application.Security;

public static class RegistrationRolePolicy
{
    public static bool TryParse(string? value, out UserRole role)
    {
        if (string.Equals(value, nameof(UserRole.Employer), StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole.Employer;
            return true;
        }

        if (string.Equals(value, nameof(UserRole.JobSeeker), StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole.JobSeeker;
            return true;
        }

        role = default;
        return false;
    }
}
