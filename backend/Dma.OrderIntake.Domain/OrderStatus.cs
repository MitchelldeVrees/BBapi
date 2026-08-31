namespace Dma.OrderIntake.Domain;

// The reliable-submission lifecycle: Submitted -> StagingPending happens
// synchronously (same transaction as the outbox message). StagingPending ->
// StagingInProgress -> StagedInEmsx (or StagingFailed) happens asynchronously,
// driven by the outbox worker calling IEmsxStagingGateway. Still not the full
// state machine from the spec (Validating, Accepted, ManualReviewRequired,
// etc.) — that's later.
public enum OrderStatus
{
    Draft,
    Submitted,
    StagingPending,
    StagingInProgress,
    StagedInEmsx,
    StagingFailed
}
