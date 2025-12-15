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

    public string? PhoneNumber { get; set; }   // opcional, pero requerido si no hay Email
    public string? Email { get; set; }         // opcional, pero requerido si no hay PhoneNumber
    public string? PasswordHash { get; set; }  // opcional siempre

    // Navs
    public List<Device> Devices { get; set; } = new();
    public List<UserSession> Sessions { get; set; } = new();
    public IdentityKey? IdentityKey { get; set; }
}
