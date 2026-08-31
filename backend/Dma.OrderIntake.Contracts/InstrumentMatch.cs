namespace Dma.OrderIntake.Contracts;

// One security-master hit. InstrumentId is a stable identifier (fixed in the
// mock dataset today) — it's what gets passed to the confirm-instrument step.
public record InstrumentMatch(
    Guid InstrumentId,
    string Isin,
    string Mic,
    string Name,
    string Currency,
    string SecurityType,
    string Ticker,
    string Figi);
