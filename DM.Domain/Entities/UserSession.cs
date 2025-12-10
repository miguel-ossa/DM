using System;

namespace DM.Domain.Entities;

public class UserSession
{
    public Guid SessionId { get; set; }   // o int, pero un Guid encaja bien

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? DeviceId { get; set; }
    public Device? Device { get; set; }

    public string AuthTokenHash { get; set; } = null!;
    public string RefreshTokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
