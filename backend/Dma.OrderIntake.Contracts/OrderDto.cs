namespace Dma.OrderIntake.Contracts;

// The API/wire shape of an Order. Kept separate from Domain.Order so the
// public contract can evolve independently of the internal model.
public record OrderDto(
    Guid Id,
    string InternalOrderNumber,
    Guid CorrelationId,
    Guid CustomerId,
    string CustomerName,
    Guid AccountId,
    string AccountCode,
    Guid? InstrumentId,
    string? Figi,
    string? InstrumentName,
    string Side,
    decimal Quantity,
    string OrderType,
    decimal? LimitPrice,
    string Currency,
    string? CustomerReference,
    string? Notes,
    string Status,
    DateTime CreatedAtUtc,
    string? EmsxSequenceNumber,
    string? StagingFailureReason);
