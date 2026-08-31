namespace Dma.OrderIntake.Contracts;

public record AuditEventDto(
    string EventType,
    string Description,
    DateTime OccurredAtUtc,
    string ActorUser,
    Guid CorrelationId,
    string? Reference);
