namespace Pursuit.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;

    public RefreshToken? ReplacedByToken { get; set; }
}