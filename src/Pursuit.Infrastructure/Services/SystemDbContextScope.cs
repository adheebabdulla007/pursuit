using Pursuit.Application.Interfaces;

namespace Pursuit.Infrastructure.Services;

public class SystemDbContextScope : IDbContextScope
{
    public Guid? TenantId => null;
    public Guid? CurrentUserId => null;
}