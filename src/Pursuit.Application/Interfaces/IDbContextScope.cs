namespace Pursuit.Application.Interfaces;

public interface IDbContextScope
{
    Guid? TenantId { get; }
    Guid? CurrentUserId { get; }
}