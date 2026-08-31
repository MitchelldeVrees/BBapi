using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Infrastructure.DmaConnect;

// Stands in for the real dmaConnect integration. Hardcoded demo data lets the
// rest of the app (Application use cases, Api, Angular) be built against the
// real IDmaConnectClient shape before dmaConnect itself exists. IDs are fixed
// literals (not Guid.NewGuid()) so orders created against these accounts keep
// referencing the same account across restarts.
public class MockDmaConnectClient : IDmaConnectClient
{
    private static readonly Guid DmaDemoAssetManagerId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ExamplePensionFundId = Guid.Parse("11111111-0000-0000-0000-000000000002");

    private static readonly IReadOnlyList<CustomerAccount> Accounts =
    [
        new CustomerAccount(
            DmaDemoAssetManagerId,
            "DMA Demo Asset Manager",
            Guid.Parse("22222222-0000-0000-0000-000000000001"),
            "FUND-EUR-001",
            "EUR"),
        new CustomerAccount(
            DmaDemoAssetManagerId,
            "DMA Demo Asset Manager",
            Guid.Parse("22222222-0000-0000-0000-000000000002"),
            "FUND-EUR-002",
            "EUR"),
        new CustomerAccount(
            DmaDemoAssetManagerId,
            "DMA Demo Asset Manager",
            Guid.Parse("22222222-0000-0000-0000-000000000003"),
            "FUND-USD-001",
            "USD"),
        new CustomerAccount(
            ExamplePensionFundId,
            "Example Pension Fund",
            Guid.Parse("22222222-0000-0000-0000-000000000004"),
            "PENSION-EUR-001",
            "EUR"),
        new CustomerAccount(
            ExamplePensionFundId,
            "Example Pension Fund",
            Guid.Parse("22222222-0000-0000-0000-000000000005"),
            "PENSION-USD-001",
            "USD"),
    ];

    public Task<IReadOnlyList<CustomerAccount>> GetAccountsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Accounts);
}
