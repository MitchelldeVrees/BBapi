using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Create_ValidLimitOrder_DefaultsToDraftStatus()
    {
        var order = OrderTestFactory.Create(orderType: OrderType.Limit, limitPrice: 125.50m);

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.NotEqual(Guid.Empty, order.CorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(order.InternalOrderNumber));
    }

    [Fact]
    public void Create_QuantityZeroOrLess_Throws()
    {
        Assert.Throws<DomainException>(() => OrderTestFactory.Create(quantity: 0));
    }

    [Fact]
    public void Create_MarketOrderWithLimitPrice_Throws()
    {
        Assert.Throws<DomainException>(() => OrderTestFactory.Create(orderType: OrderType.Market, limitPrice: 100m));
    }

    [Fact]
    public void Create_LimitOrderWithoutLimitPrice_Throws()
    {
        Assert.Throws<DomainException>(() => OrderTestFactory.Create(orderType: OrderType.Limit, limitPrice: null));
    }

    [Fact]
    public void Create_BlankAccountCode_Throws()
    {
        Assert.Throws<DomainException>(() => OrderTestFactory.Create(accountCode: " "));
    }

    [Fact]
    public void Submit_WithoutConfirmedInstrument_Throws()
    {
        var order = OrderTestFactory.Create();

        Assert.Throws<DomainException>(() => order.Submit("key-1"));
    }

    [Fact]
    public void Submit_WithConfirmedInstrument_TransitionsToSubmittedAndStoresKey()
    {
        var order = OrderTestFactory.CreateAndConfirm();

        order.Submit("key-1");

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal("key-1", order.IdempotencyKey);
    }

    [Fact]
    public void Submit_WithBlankIdempotencyKey_Throws()
    {
        var order = OrderTestFactory.CreateAndConfirm();

        Assert.Throws<DomainException>(() => order.Submit(" "));
    }

    [Fact]
    public void ConfirmInstrument_SnapshotsFigiAndName()
    {
        var order = OrderTestFactory.CreateAndConfirm(figi: "BBG000BPHFP7", instrumentName: "ASML Holding NV");

        Assert.Equal("BBG000BPHFP7", order.Figi);
        Assert.Equal("ASML Holding NV", order.InstrumentName);
    }
}
