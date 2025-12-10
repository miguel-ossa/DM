using System;

namespace DM.Domain.Entities;

public class User
{
    public int UserId { get; set; }

    // Maneja esto como tu “handle” único (teléfono/username)
    public string UserName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navs
    public List<Device> Devices { get; set; } = new();
    public List<UserSession> Sessions { get; set; } = new();
    public IdentityKey? IdentityKey { get; set; }
}
