namespace Pursuit.Application.Interfaces;

public interface ITenantService
{
    Guid? GetTenantId();

    bool HasTenant();
}