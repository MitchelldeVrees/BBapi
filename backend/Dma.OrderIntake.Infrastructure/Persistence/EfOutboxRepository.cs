using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dma.OrderIntake.Infrastructure.Persistence;

public class EfOutboxRepository(OrderIntakeDbContext db) : IOutboxRepository
{
    public async Task EnqueueStagingMessageAsync(Order order, OutboxMessage message, CancellationToken cancellationToken)
    {
        db.Orders.Update(order);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken); // one SaveChanges call = one atomic commit
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken) =>
        await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task SaveProcessingResultAsync(Order order, OutboxMessage message, CancellationToken cancellationToken)
    {
        db.Orders.Update(order);
        db.OutboxMessages.Update(message);
        await db.SaveChangesAsync(cancellationToken); // one SaveChanges call = one atomic commit
    }
}
