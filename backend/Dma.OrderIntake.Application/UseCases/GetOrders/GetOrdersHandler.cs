using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Application.Mapping;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.UseCases.GetOrders;

public interface IGetOrdersHandler
{
    Task<IReadOnlyList<OrderDto>> HandleAsync(CancellationToken cancellationToken);
}

public class GetOrdersHandler(IOrderRepository orderRepository) : IGetOrdersHandler
{
    public async Task<IReadOnlyList<OrderDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetAllAsync(cancellationToken);

        return orders.Select(OrderMapper.ToDto).ToList();
    }
}
