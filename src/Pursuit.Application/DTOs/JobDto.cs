using Pursuit.Domain.Enums;

namespace Pursuit.Application.DTOs;

public class JobDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public JobType JobType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}