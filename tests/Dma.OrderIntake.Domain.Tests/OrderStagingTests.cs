using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Domain.Tests;

public class OrderStagingTests
{
    private static Order SubmittedOrder()
    {
        var order = OrderTestFactory.CreateAndConfirm();
        order.Submit("key-1");
        return order;
    }

    [Fact]
    public void FullHappyPath_TransitionsThroughEveryStagingStatus()
    {
        var order = SubmittedOrder();
        Assert.Equal(OrderStatus.Submitted, order.Status);

        order.MarkStagingPending();
        Assert.Equal(OrderStatus.StagingPending, order.Status);

        order.BeginStaging();
        Assert.Equal(OrderStatus.StagingInProgress, order.Status);

        order.MarkStagedInEmsx("SIM-492823");
        Assert.Equal(OrderStatus.StagedInEmsx, order.Status);
        Assert.Equal("SIM-492823", order.EmsxSequenceNumber);
        Assert.Null(order.StagingFailureReason);
    }

    [Fact]
    public void MarkStagingFailed_FromInProgress_SetsReason()
    {
        var order = SubmittedOrder();
        order.MarkStagingPending();
        order.BeginStaging();

        order.MarkStagingFailed("EMSX unreachable.");

        Assert.Equal(OrderStatus.StagingFailed, order.Status);
        Assert.Equal("EMSX unreachable.", order.StagingFailureReason);
    }

    [Fact]
    public void MarkStagingPending_WhenNotSubmitted_Throws()
    {
        var order = OrderTestFactory.Create();

        Assert.Throws<DomainException>(order.MarkStagingPending);
    }

    [Fact]
    public void BeginStaging_WhenNotStagingPending_Throws()
    {
        var order = SubmittedOrder();

        Assert.Throws<DomainException>(order.BeginStaging);
    }

    [Fact]
    public void MarkStagedInEmsx_WhenNotInProgress_Throws()
    {
        var order = SubmittedOrder();
        order.MarkStagingPending();

        Assert.Throws<DomainException>(() => order.MarkStagedInEmsx("SIM-1"));
    }

    [Fact]
    public void ConfirmInstrument_WithoutFigi_Throws()
    {
        var order = OrderTestFactory.Create();

        Assert.Throws<DomainException>(() => order.ConfirmInstrument(Guid.NewGuid(), "", "ASML Holding NV"));
    }
}
