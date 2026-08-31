using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Application.Mapping;
using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.UseCases.CreateOrder;

public interface ICreateOrderHandler
{
    Task<OrderDto> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken);
}

public class CreateOrderHandler(IOrderRepository orderRepository, IOrderAuditTrail auditTrail) : ICreateOrderHandler
{
    public async Task<OrderDto> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderSide>(request.Side, ignoreCase: true, out var side))
        {
            throw new DomainException($"Unknown order side '{request.Side}'.");
        }

        if (!Enum.TryParse<OrderType>(request.OrderType, ignoreCase: true, out var orderType))
        {
            throw new DomainException($"Unknown order type '{request.OrderType}'.");
        }

        // All the "is this order actually valid" invariants live in Order.Create,
        // not here and not in Angular — this handler just translates the request.
        var order = Order.Create(
            request.CustomerId,
            request.CustomerName,
            request.AccountId,
            request.AccountCode,
            side,
            request.Quantity,
            orderType,
            request.LimitPrice,
            request.Currency,
            request.CustomerReference,
            request.Notes);

        await orderRepository.AddAsync(order, cancellationToken);

        await auditTrail.RecordAsync(
            OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.OrderCreated, "Order created", order.CorrelationId, order.InternalOrderNumber),
            cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
