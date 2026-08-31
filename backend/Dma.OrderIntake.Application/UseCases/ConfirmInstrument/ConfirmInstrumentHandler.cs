using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Application.Mapping;
using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.UseCases.ConfirmInstrument;

public interface IConfirmInstrumentHandler
{
    Task<OrderDto?> HandleAsync(Guid orderId, ConfirmInstrumentRequest request, CancellationToken cancellationToken);
}

// The explicit "yes" step. Resolving an instrument never calls this on its
// own — a human always has to invoke it separately, even for a single match.
public class ConfirmInstrumentHandler(IOrderRepository orderRepository, IOrderAuditTrail auditTrail) : IConfirmInstrumentHandler
{
    public async Task<OrderDto?> HandleAsync(Guid orderId, ConfirmInstrumentRequest request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.ConfirmInstrument(request.InstrumentId, request.Figi, request.InstrumentName);
        await orderRepository.UpdateAsync(order, cancellationToken);

        await auditTrail.RecordAsync(
            OrderAuditEvent.Record(
                order.Id,
                OrderAuditEventTypes.InstrumentConfirmed,
                $"Instrument confirmed: {request.InstrumentName} ({request.Figi})",
                order.CorrelationId,
                request.Figi),
            cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
