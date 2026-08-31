using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.Abstractions;

public interface IOrderAuditTrail
{
    Task RecordAsync(OrderAuditEvent auditEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderAuditEvent>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
