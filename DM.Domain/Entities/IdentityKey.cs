using System;

namespace DM.Domain.Entities;

public class IdentityKey
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string PublicKey { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
