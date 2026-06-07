using Pursuit.Domain.Enums;

namespace Pursuit.Application.DTOs;

public class UpdateJobDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public JobType JobType { get; set; }
    public bool IsActive { get; set; }
}