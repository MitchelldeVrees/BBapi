using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Application.Mapping;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.UseCases.GetOrderById;

public interface IGetOrderByIdHandler
{
    Task<OrderDto?> HandleAsync(Guid id, CancellationToken cancellationToken);
}

public class GetOrderByIdHandler(IOrderRepository orderRepository) : IGetOrderByIdHandler
{
    public async Task<OrderDto?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);

        return order is null ? null : OrderMapper.ToDto(order);
    }
}
