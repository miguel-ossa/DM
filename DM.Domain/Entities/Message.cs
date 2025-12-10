using System;

namespace DM.Domain.Entities;

public class Message
{
    public Guid MessageId { get; set; }    // GUID como comentas

    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public int SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    public string ClientMessageId { get; set; } = null!;  // para deduplicación

    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public MessagePayloadType PayloadType { get; set; }

    public string CipherText { get; set; } = null!;
    public string CipherMetadata { get; set; } = null!;   // JSON o similar

    public List<MessageDelivery> Deliveries { get; set; } = new();
}
