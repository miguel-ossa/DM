using System;

namespace DM.Domain.Entities;

public class SessionKey
{
    public Guid SessionKeyId { get; set; }

    public int? ChatId { get; set; }
    public Chat? Chat { get; set; }

    public int LocalDeviceId { get; set; }
    public Device LocalDevice { get; set; } = null!;

    public int RemoteDeviceId { get; set; }
    public Device RemoteDevice { get; set; } = null!;

    public string KeyMaterial { get; set; } = null!;  // opaco para el servidor

    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
