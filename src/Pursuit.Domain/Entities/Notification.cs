namespace Pursuit.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;

    // Navigation properties
    public User User { get; set; } = null!;
}