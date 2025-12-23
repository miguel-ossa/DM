namespace DM.Infrastructure.EventBus;

public class EventBusEvent
{
    public long Id { get; set; }                // BIGINT AUTO_INCREMENT

    public string EventType { get; set; } = null!;

    public string Payload { get; set; } = null!; // JSON como string

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? CorrelationId { get; set; }
}
