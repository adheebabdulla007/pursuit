using Pursuit.Domain.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace Pursuit.Domain.Entities;

public class Job : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public JobType JobType { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}