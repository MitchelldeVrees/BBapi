using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Domain.Tests;

// Keeps test call sites readable as Order.Create()'s parameter list grows.
internal static class OrderTestFactory
{
    public static Order Create(
        OrderSide side = OrderSide.Buy,
        decimal quantity = 100,
        OrderType orderType = OrderType.Market,
        decimal? limitPrice = null,
        string currency = "USD",
        string accountCode = "FUND-EUR-001") =>
        Order.Create(
            Guid.NewGuid(),
            "Demo Customer",
            Guid.NewGuid(),
            accountCode,
            side,
            quantity,
            orderType,
            limitPrice,
            currency,
            customerReference: null,
            notes: null);

    public static Order CreateAndConfirm(string figi = "BBG000BPHFP7", string instrumentName = "ASML Holding NV")
    {
        var order = Create(orderType: OrderType.Market);
        order.ConfirmInstrument(Guid.NewGuid(), figi, instrumentName);
        return order;
    }
}
