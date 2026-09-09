using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>
/// An opaque, long-lived token that lets the client silently obtain a new short-lived JWT without
/// forcing the user to log in again. Only the SHA-256 hash is stored — the plaintext value is
/// handed to the client once (at login/refresh) and never persisted.
/// </summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
