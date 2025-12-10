using System;

namespace DM.Domain.Entities;

public class DeviceKey
{
    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public string PublicKey { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
