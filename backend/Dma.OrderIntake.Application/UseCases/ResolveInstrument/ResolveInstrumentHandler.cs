using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.UseCases.ResolveInstrument;

public interface IResolveInstrumentHandler
{
    Task<InstrumentResolutionResult> HandleAsync(InstrumentResolutionRequest request, CancellationToken cancellationToken);
}

// Thin today (the resolver is mocked), but this is the seam: Api never talks
// to IInstrumentResolver directly, same discipline as everywhere else.
public class ResolveInstrumentHandler(IInstrumentResolver instrumentResolver) : IResolveInstrumentHandler
{
    public Task<InstrumentResolutionResult> HandleAsync(InstrumentResolutionRequest request, CancellationToken cancellationToken) =>
        instrumentResolver.ResolveAsync(request, cancellationToken);
}
