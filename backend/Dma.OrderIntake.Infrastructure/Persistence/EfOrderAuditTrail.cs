using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dma.OrderIntake.Infrastructure.Persistence;

public class EfOrderAuditTrail(OrderIntakeDbContext db) : IOrderAuditTrail
{
    public async Task RecordAsync(OrderAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        db.OrderAuditEvents.Add(auditEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderAuditEvent>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await db.OrderAuditEvents
            .Where(e => e.OrderId == orderId)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);
}
