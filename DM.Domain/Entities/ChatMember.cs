using System;

namespace DM.Domain.Entities;

public class ChatMember
{
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ChatMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? MutedUntil { get; set; }
}
