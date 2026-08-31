namespace Dma.OrderIntake.Domain;

public static class OutboxMessageTypes
{
    public const string StageOrder = "StageOrder";
}

// The reliability mechanism: an Order's status change and this message are
// written in the same database transaction (see Order.MarkStagingPending +
// SubmitOrderHandler), so "the order says StagingPending" and "there is work
// queued to actually stage it" can never disagree. A background worker polls
// for unprocessed messages and drives the actual (mocked) EMSX call — never
// the HTTP request that submitted the order.
public class OutboxMessage
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Type { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public bool IsProcessed => ProcessedAtUtc is not null;

    private OutboxMessage()
    {
    }

    public static OutboxMessage CreateStageOrderMessage(Guid orderId) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        Type = OutboxMessageTypes.StageOrder,
        CreatedAtUtc = DateTime.UtcNow,
        AttemptCount = 0,
        ProcessedAtUtc = null,
        LastError = null,
    };

    public void RecordAttemptStarted()
    {
        AttemptCount++;
    }

    // "Processed" means the worker is done attempting this message, not that
    // it succeeded — LastError is left as-is so a terminally-failed message
    // still shows what went wrong. See RecordFailure.
    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void RecordFailure(string error)
    {
        LastError = error;
    }
}
