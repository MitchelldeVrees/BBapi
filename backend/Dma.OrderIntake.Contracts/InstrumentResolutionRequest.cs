namespace Dma.OrderIntake.Contracts;

// Mic is optional: omitting it (or leaving it blank) asks the resolver to
// return every listing for the ISIN, which is how a genuinely ambiguous
// (multi-market) result gets surfaced instead of guessed at.
public record InstrumentResolutionRequest(string Isin, string? Mic);
