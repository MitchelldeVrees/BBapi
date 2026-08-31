using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.UseCases.GetAccounts;

public interface IGetAccountsHandler
{
    Task<IReadOnlyList<CustomerAccount>> HandleAsync(CancellationToken cancellationToken);
}

// Thin today (dmaConnect is mocked), but this is the seam the rest of the app
// depends on — Api never talks to IDmaConnectClient directly.
public class GetAccountsHandler(IDmaConnectClient dmaConnectClient) : IGetAccountsHandler
{
    public Task<IReadOnlyList<CustomerAccount>> HandleAsync(CancellationToken cancellationToken) =>
        dmaConnectClient.GetAccountsAsync(cancellationToken);
}
