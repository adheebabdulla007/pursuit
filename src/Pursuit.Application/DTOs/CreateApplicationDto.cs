namespace Pursuit.Application.DTOs;

public class CreateApplicationDto
{
    public Guid JobId { get; set; }
    public string ResumeUrl { get; set; } = string.Empty;
}