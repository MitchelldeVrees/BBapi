using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.Abstractions;

// Port: Application depends only on this interface, never on EF Core or SQLite
// directly. Infrastructure provides the implementation.
public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task UpdateAsync(Order order, CancellationToken cancellationToken);
}
