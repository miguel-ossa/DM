using System;

namespace DM.Domain.Entities;

public class Device
{
    public int DeviceId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Platform { get; set; } = null!;      // "Android", "iOS", etc.
    public string? PushToken { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsTrusted { get; set; }

    public DeviceKey? DeviceKey { get; set; }
    public List<UserSession> Sessions { get; set; } = new();
}
