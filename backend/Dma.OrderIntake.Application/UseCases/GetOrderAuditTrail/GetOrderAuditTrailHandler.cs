using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Application.Mapping;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.UseCases.GetOrderAuditTrail;

public interface IGetOrderAuditTrailHandler
{
    Task<IReadOnlyList<AuditEventDto>> HandleAsync(Guid orderId, CancellationToken cancellationToken);
}

public class GetOrderAuditTrailHandler(IOrderAuditTrail auditTrail) : IGetOrderAuditTrailHandler
{
    public async Task<IReadOnlyList<AuditEventDto>> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var events = await auditTrail.GetForOrderAsync(orderId, cancellationToken);

        return events.Select(OrderMapper.ToDto).ToList();
    }
}
