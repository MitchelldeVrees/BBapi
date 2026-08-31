namespace Dma.OrderIntake.Contracts;

public record InstrumentResolutionResult(
    InstrumentResolutionStatus Status,
    string? ErrorMessage,
    IReadOnlyList<InstrumentMatch> Matches);
