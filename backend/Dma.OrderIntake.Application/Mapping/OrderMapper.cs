using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.Mapping;

public static class OrderMapper
{
    public static OrderDto ToDto(Order order) => new(
        order.Id,
        order.InternalOrderNumber,
        order.CorrelationId,
        order.CustomerId,
        order.CustomerName,
        order.AccountId,
        order.AccountCode,
        order.InstrumentId,
        order.Figi,
        order.InstrumentName,
        order.Side.ToString(),
        order.Quantity,
        order.OrderType.ToString(),
        order.LimitPrice,
        order.Currency,
        order.CustomerReference,
        order.Notes,
        order.Status.ToString(),
        order.CreatedAtUtc,
        order.EmsxSequenceNumber,
        order.StagingFailureReason);

    public static AuditEventDto ToDto(OrderAuditEvent auditEvent) => new(
        auditEvent.EventType,
        auditEvent.Description,
        auditEvent.OccurredAtUtc,
        auditEvent.ActorUser,
        auditEvent.CorrelationId,
        auditEvent.Reference);
}
