using System.Text.Json.Serialization;

namespace Dma.OrderIntake.Contracts;

// Serialized as a string (not the default numeric enum value) so it reads the
// same way every other status-like field in this API does.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstrumentResolutionStatus
{
    // Exactly one confident match. Still requires an explicit confirm step —
    // resolving is never the same as confirming.
    Resolved,

    // More than one candidate matched; the caller must show all of them and
    // let the customer pick. Never auto-select the first one.
    MultipleMatches,

    // Input was well-formed but nothing in the security master matched it.
    NotFound,

    // ISIN format/check digit or MIC format was invalid.
    Invalid
}
