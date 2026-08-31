using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.Abstractions;

// Port: stands in for the future dmaConnect integration (accounts, custodian
// mapping, settlement context, account validation). Application and Api code
// depend only on this interface, never on how accounts are actually sourced —
// MockDmaConnectClient is a placeholder Infrastructure will swap out later.
public interface IDmaConnectClient
{
    Task<IReadOnlyList<CustomerAccount>> GetAccountsAsync(CancellationToken cancellationToken);
}
