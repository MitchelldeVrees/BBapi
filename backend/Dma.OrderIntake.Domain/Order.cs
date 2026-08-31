namespace Dma.OrderIntake.Domain;

// Rich entity: state only changes through Create()/Submit()/the staging
// transitions below, so every Order that exists in memory has already
// satisfied its invariants. No HTTP, no EF Core attributes, no Bloomberg SDK
// types, no knowledge of how it is persisted or exposed.
public class Order
{
    public Guid Id { get; private set; }

    public string InternalOrderNumber { get; private set; } = null!;

    public Guid CustomerId { get; private set; }

    // Snapshotted from dmaConnect at creation time, same reasoning as
    // AccountCode below — the order overview/detail screens need it and
    // shouldn't have to look it up again.
    public string CustomerName { get; private set; } = null!;

    public Guid AccountId { get; private set; }

    // Snapshotted from the account chosen at creation time (dmaConnect owns
    // the live mapping; this is what EMSX staging actually needs to send).
    public string AccountCode { get; private set; } = null!;

    // Null until an instrument-resolution step confirms it. An order can be
    // created without one, but cannot be submitted without one.
    public Guid? InstrumentId { get; private set; }

    // Snapshotted from the confirmed match — this, not InstrumentId, is what
    // EMSX staging actually needs to send.
    public string? Figi { get; private set; }

    public string? InstrumentName { get; private set; }

    // Ties every audit event for this order together, including the ones
    // recorded later by the outbox worker — see OrderAuditEvent.
    public Guid CorrelationId { get; private set; }

    public OrderSide Side { get; private set; }

    public decimal Quantity { get; private set; }

    public OrderType OrderType { get; private set; }
    public decimal? LimitPrice { get; private set; }

    public string Currency { get; private set; } = null!;

    public string? CustomerReference { get; private set; }
    public string? Notes { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Set once the outbox worker gets a successful EmsxStageResult back.
    public string? EmsxSequenceNumber { get; private set; }

    // Set if staging fails — surfaced so someone can see why, not just that it failed.
    public string? StagingFailureReason { get; private set; }

    // The key the customer's (or Angular's) submission request carried. A
    // second Submit() call with this exact key is a safe retry, not a second
    // submission — see SubmitOrderHandler, which checks this before calling
    // Submit() at all.
    public string? IdempotencyKey { get; private set; }

    // EF Core materializes existing rows through this; application code can't
    // use it to bypass the invariants enforced in Create().
    private Order()
    {
    }

    public static Order Create(
        Guid customerId,
        string customerName,
        Guid accountId,
        string accountCode,
        OrderSide side,
        decimal quantity,
        OrderType orderType,
        decimal? limitPrice,
        string currency,
        string? customerReference,
        string? notes)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (orderType == OrderType.Market && limitPrice is not null)
        {
            throw new DomainException("Market orders must not have a limit price.");
        }

        if (orderType == OrderType.Limit && limitPrice is null)
        {
            throw new DomainException("Limit orders require a limit price.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        if (string.IsNullOrWhiteSpace(accountCode))
        {
            throw new DomainException("Account code is required.");
        }

        return new Order
        {
            Id = Guid.NewGuid(),
            InternalOrderNumber = GenerateInternalOrderNumber(),
            CorrelationId = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerName = customerName,
            AccountId = accountId,
            AccountCode = accountCode,
            InstrumentId = null,
            Figi = null,
            InstrumentName = null,
            Side = side,
            Quantity = quantity,
            OrderType = orderType,
            LimitPrice = limitPrice,
            Currency = currency,
            CustomerReference = customerReference,
            Notes = notes,
            Status = OrderStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void ConfirmInstrument(Guid instrumentId, string figi, string instrumentName)
    {
        if (string.IsNullOrWhiteSpace(figi))
        {
            throw new DomainException("FIGI is required to confirm an instrument.");
        }

        InstrumentId = instrumentId;
        Figi = figi;
        InstrumentName = instrumentName;
    }

    public void Submit(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainException("Idempotency key is required to submit an order.");
        }

        if (InstrumentId is null)
        {
            throw new DomainException("Order cannot be submitted without a confirmed instrument.");
        }

        if (Status != OrderStatus.Draft)
        {
            throw new DomainException($"Order cannot be submitted from status '{Status}'.");
        }

        IdempotencyKey = idempotencyKey;
        Status = OrderStatus.Submitted;
    }

    // Submitted -> StagingPending happens synchronously, in the same
    // transaction as the outbox message that queues the EMSX staging work.
    public void MarkStagingPending()
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new DomainException($"Order cannot move to StagingPending from status '{Status}'.");
        }

        Status = OrderStatus.StagingPending;
    }

    // Everything below this point happens asynchronously, driven by the
    // outbox worker — never inline with the customer's HTTP request.
    public void BeginStaging()
    {
        if (Status != OrderStatus.StagingPending)
        {
            throw new DomainException($"Order cannot begin staging from status '{Status}'.");
        }

        Status = OrderStatus.StagingInProgress;
    }

    public void MarkStagedInEmsx(string sequenceNumber)
    {
        if (Status != OrderStatus.StagingInProgress)
        {
            throw new DomainException($"Order cannot be marked staged from status '{Status}'.");
        }

        Status = OrderStatus.StagedInEmsx;
        EmsxSequenceNumber = sequenceNumber;
        StagingFailureReason = null;
    }

    public void MarkStagingFailed(string reason)
    {
        if (Status != OrderStatus.StagingInProgress)
        {
            throw new DomainException($"Order cannot be marked failed from status '{Status}'.");
        }

        Status = OrderStatus.StagingFailed;
        StagingFailureReason = reason;
    }

    private static string GenerateInternalOrderNumber()
    {
        // Simple and readable for this step. A real sequence/format (matching
        // whatever the DMATracker migration needs) comes later.
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
