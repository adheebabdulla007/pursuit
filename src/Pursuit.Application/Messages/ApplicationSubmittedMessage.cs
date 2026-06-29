namespace Pursuit.Application.Messages;

public sealed class ApplicationSubmittedMessage
{
    public Guid ApplicationId { get; init; }
    public string ApplicantName { get; init; } = string.Empty;
    public string ApplicantEmail { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string EmployerEmail { get; init; } = string.Empty;
}