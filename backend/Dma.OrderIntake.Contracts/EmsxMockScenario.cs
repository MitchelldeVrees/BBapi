using System.Text.Json.Serialization;

namespace Dma.OrderIntake.Contracts;

// Admin-only (see IEmsxMockScenarioStore) test-scenario switch for
// MockEmsxStagingGateway. None of this exists once a real EMSX integration
// replaces the mock — it's testing/ops tooling bolted onto the mock, not a
// real business concept.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmsxMockScenario
{
    Success,
    DelayedSuccess,
    BloombergRejection,
    TemporaryConnectionFailure,
    TimeoutUnknownState,
    InvalidInstrument,
    InvalidAccount,
    DuplicateRequest,
}

public record EmsxMockScenarioSettings(EmsxMockScenario Scenario, int ArtificialDelayMs);
