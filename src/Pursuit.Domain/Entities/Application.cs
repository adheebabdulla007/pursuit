using Pursuit.Domain.Enums;

namespace Pursuit.Domain.Entities;

public class Application : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ApplicantId { get; set; }
    public string ResumeUrl { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    // Navigation properties
    public Job Job { get; set; } = null!;

    public User Applicant { get; set; } = null!;
}