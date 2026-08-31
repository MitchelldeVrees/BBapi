namespace Dma.OrderIntake.Contracts;

// The explicit "yes, this is the right instrument" step. Deliberately a
// separate call from resolve — resolving never confirms anything by itself.
//
// Figi/InstrumentName are snapshotted from the resolved match the caller
// already has, rather than looked up again here — it's what EMSX staging and
// the order overview/detail screens need later.
public record ConfirmInstrumentRequest(Guid InstrumentId, string Figi, string InstrumentName);
