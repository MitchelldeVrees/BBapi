using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.Abstractions;

// Port: stands in for the real security-master / OpenFIGI integration.
// MockInstrumentResolver is the first (and, for now, only) implementation.
public interface IInstrumentResolver
{
    Task<InstrumentResolutionResult> ResolveAsync(
        InstrumentResolutionRequest request,
        CancellationToken cancellationToken);
}
