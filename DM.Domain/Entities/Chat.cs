using System;

namespace DM.Domain.Entities;

public class Chat
{
    public int ChatId { get; set; }

    public ChatType Type { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Title { get; set; }        // solo grupos
    public bool IsEncrypted { get; set; }

    public List<ChatMember> Members { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}
