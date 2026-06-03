using Pursuit.Domain.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace Pursuit.Domain.Entities;

public class User : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}