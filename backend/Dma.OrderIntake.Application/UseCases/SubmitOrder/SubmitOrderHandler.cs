using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Application.Mapping;
using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.UseCases.SubmitOrder;

public interface ISubmitOrderHandler
{
    Task<OrderDto?> HandleAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken);
}

// This is deliberately NOT where EMSX gets called. It only ever does a single
// database transaction (order status + outbox message) and returns — the
// actual staging call happens later, out of band, in OutboxProcessorWorker.
// That's what makes a dropped connection to EMSX safe instead of a double
// submission risk.
//
// The idempotency-key check below is the other half of that safety: if
// Angular (or a flaky network) sends the exact same submit request twice,
// the second call must not attempt Submit() again (it would throw — the
// order has already moved past Draft) and must not enqueue a second outbox
// message. It just returns the order as it already stands.
public class SubmitOrderHandler(
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IOrderAuditTrail auditTrail) : ISubmitOrderHandler
{
    public async Task<OrderDto?> HandleAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.IdempotencyKey == idempotencyKey)
        {
            return OrderMapper.ToDto(order);
        }

        order.Submit(idempotencyKey);
        order.MarkStagingPending();

        var outboxMessage = OutboxMessage.CreateStageOrderMessage(order.Id);
        await outboxRepository.EnqueueStagingMessageAsync(order, outboxMessage, cancellationToken);

        await auditTrail.RecordAsync(
            OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.OrderSubmitted, "Order submitted", order.CorrelationId),
            cancellationToken);
        await auditTrail.RecordAsync(
            OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.StagingPending, "Staging pending", order.CorrelationId),
            cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
