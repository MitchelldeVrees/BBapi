using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.Abstractions;

// This, not any Bloomberg SDK type (BloombergSession, BloombergRequest,
// BloombergElement, ...), is what the rest of the application knows about.
// Never serialized over HTTP — it's a pure in-process port contract between
// the outbox worker and whatever gateway implementation is registered.
public record StageOrderCommand(
    Guid InternalOrderId,
    string OrderReferenceId,
    string Figi,
    OrderSide Side,
    decimal Quantity,
    OrderType OrderType,
    decimal? LimitPrice,
    string Account);

public record EmsxStageResult(bool Success, string? SequenceNumber, string? ErrorMessage);

// Port: stands in for the real Bloomberg EMSX integration. SIMULATION ONLY
// for now — MockEmsxStagingGateway never talks to a real venue. Scope for
// Goal 1 is strictly "create the parent order in EMSX" — no broker route, no
// trade.
public interface IEmsxStagingGateway
{
    Task<EmsxStageResult> StageOrderAsync(StageOrderCommand command, CancellationToken cancellationToken);
}
