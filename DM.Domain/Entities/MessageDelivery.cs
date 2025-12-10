using System;

namespace DM.Domain.Entities;

public class MessageDelivery
{
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;   // destinatario

    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public long ServerSequence { get; set; }  // para ordenar eventos
}
