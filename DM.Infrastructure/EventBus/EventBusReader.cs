using DM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DM.Infrastructure.EventBus;

public sealed class EventBusReader
{
    private readonly AppDbContext _db;

    public EventBusReader(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Devuelve eventos pendientes (processed_at IS NULL)
    /// </summary>
    public async Task<IReadOnlyList<EventBusEvent>> GetPendingAsync(
        int batchSize = 50,
        CancellationToken ct = default)
    {
        return await _db.EventBusEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.Id)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Marca un evento como procesado correctamente
    /// </summary>
    public async Task MarkAsProcessedAsync(
        long eventId,
        CancellationToken ct = default)
    {
        await _db.EventBusEvents
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(
                    e => e.ProcessedAt,
                    _ => DateTime.UtcNow),
                ct);
    }

    /// <summary>
    /// Incrementa el contador de reintentos (sin backoff)
    /// </summary>
    public async Task IncrementRetryAsync(
        long eventId,
        CancellationToken ct = default)
    {
        await _db.EventBusEvents
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(
                        e => e.RetryCount,
                        e => e.RetryCount + 1),
                ct);
    }
}



// Ejemplo de uso:
/*
var events = await eventBusReader.GetPendingAsync();

foreach (var evt in events)
{
    try
    {
        switch (evt.EventType)
        {
            case "UserRegisteredEvent":
                var data = JsonSerializer.Deserialize<UserRegisteredEvent>(evt.Payload);
                // lógica de consumo
                break;
        }

        await eventBusReader.MarkAsProcessedAsync(evt.Id);
    }
    catch
    {
        await eventBusReader.IncrementRetryAsync(evt.Id);
        throw;
    }
}

*/