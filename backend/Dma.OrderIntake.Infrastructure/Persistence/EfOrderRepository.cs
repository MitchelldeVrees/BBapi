using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dma.OrderIntake.Infrastructure.Persistence;

public class EfOrderRepository(OrderIntakeDbContext db) : IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Orders.OrderByDescending(o => o.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        // Update() is safe whether or not `order` is already tracked by this
        // DbContext (it will be, in practice — same request scope as whatever
        // loaded it via GetByIdAsync).
        db.Orders.Update(order);
        await db.SaveChangesAsync(cancellationToken);
    }
}
