namespace Dma.OrderIntake.Domain;

public static class OrderAuditEventTypes
{
    public const string OrderCreated = "OrderCreated";
    public const string InstrumentConfirmed = "InstrumentConfirmed";
    public const string OrderSubmitted = "OrderSubmitted";
    public const string StagingPending = "StagingPending";
    public const string StagingStarted = "StagingStarted";
    public const string StagedInEmsx = "StagedInEmsx";
    public const string StagingFailed = "StagingFailed";
}

// One append-only entry in an order's audit trail. Covers what the spec's
// audit requirements ask for: order ID, status transitions, actor, timestamp,
// correlation ID, and whatever reference is relevant to that specific event
// (internal order number, FIGI, EMSX sequence number, ...).
public class OrderAuditEvent
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string EventType { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    // No real identity/auth yet (IdentityServer is still out of scope) — this
    // is a placeholder a later step wires up to the actual signed-in user.
    public string ActorUser { get; private set; } = null!;

    public Guid CorrelationId { get; private set; }

    public string? Reference { get; private set; }

    private OrderAuditEvent()
    {
    }

    public static OrderAuditEvent Record(
        Guid orderId,
        string eventType,
        string description,
        Guid correlationId,
        string? reference = null,
        string actorUser = "system") => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        EventType = eventType,
        Description = description,
        OccurredAtUtc = DateTime.UtcNow,
        ActorUser = actorUser,
        CorrelationId = correlationId,
        Reference = reference,
    };
}
