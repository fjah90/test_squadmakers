namespace JokesApi.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow <= ExpiresAt;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
} 