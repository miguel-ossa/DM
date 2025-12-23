using System.Text.Json;
using DM.Infrastructure.Data;

namespace DM.Infrastructure.EventBus;

public sealed class EventBusWriter
{
    private readonly AppDbContext _db;

    public EventBusWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(
        string eventType,
        object payload,
        string? correlationId = null)
    {
        var evt = new EventBusEvent
        {
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        _db.EventBusEvents.Add(evt);
        await _db.SaveChangesAsync();
    }
}


// Ejemplo de uso:
/*
var user = User.Register(...);

db.Users.Add(user);
await db.SaveChangesAsync();

foreach (var domainEvent in user.DomainEvents)
{
    await eventBusWriter.EnqueueAsync(
        domainEvent.GetType().Name,
        domainEvent
    );
}

user.ClearDomainEvents();


*/