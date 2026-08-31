using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Infrastructure.InstrumentResolution;

// Stands in for the real security-master / OpenFIGI integration. Hardcoded
// demo data, but the resolution flow itself (validate ISIN, check digit,
// normalize MIC, search, never auto-pick when ambiguous) is the real thing —
// a later real resolver only swaps out where SecurityMaster comes from.
public class MockInstrumentResolver : IInstrumentResolver
{
    // Fixed literal IDs (not Guid.NewGuid()) so a confirmed instrument keeps
    // pointing at the same record across restarts — same reasoning as
    // MockDmaConnectClient's account/customer IDs.
    private static readonly IReadOnlyList<InstrumentMatch> SecurityMaster =
    [
        new InstrumentMatch(
            Guid.Parse("33333333-0000-0000-0000-000000000001"),
            "NL0010273215",
            "XAMS",
            "ASML Holding NV",
            "EUR",
            "Common Stock",
            "ASML",
            "BBG000BPHFP7"),

        // Deliberately dual-listed: same ISIN, two MICs. Resolving by ISIN
        // alone (no MIC) must return both and let the customer choose.
        new InstrumentMatch(
            Guid.Parse("33333333-0000-0000-0000-000000000002"),
            "NLNOVA000012",
            "XAMS",
            "Nova Renewables N.V.",
            "EUR",
            "Common Stock",
            "NOVA",
            "BBG00NVAMS01"),
        new InstrumentMatch(
            Guid.Parse("33333333-0000-0000-0000-000000000003"),
            "NLNOVA000012",
            "XLON",
            "Nova Renewables N.V.",
            "GBP",
            "Common Stock",
            "NOVL",
            "BBG00NVLON01"),
    ];

    public Task<InstrumentResolutionResult> ResolveAsync(
        InstrumentResolutionRequest request,
        CancellationToken cancellationToken)
    {
        // ISIN validaten + check digit controleren.
        if (!Isin.TryParse(request.Isin, out var isin, out var isinError))
        {
            return Task.FromResult(Invalid(isinError!));
        }

        // MIC normaliseren (optional: omitting it means "search across markets").
        Mic? mic = null;
        if (!string.IsNullOrWhiteSpace(request.Mic))
        {
            if (!Mic.TryParse(request.Mic, out var parsedMic, out var micError))
            {
                return Task.FromResult(Invalid(micError!));
            }

            mic = parsedMic;
        }

        // Mock security master zoeken.
        var matches = SecurityMaster
            .Where(m => m.Isin == isin.Value)
            .Where(m => mic is null || m.Mic == mic.Value.Value)
            .ToList();

        // Resultaat terugsturen — never collapse multiple matches to one.
        var result = matches.Count switch
        {
            0 => new InstrumentResolutionResult(InstrumentResolutionStatus.NotFound, "No instrument found for the given ISIN/MIC.", []),
            1 => new InstrumentResolutionResult(InstrumentResolutionStatus.Resolved, null, matches),
            _ => new InstrumentResolutionResult(InstrumentResolutionStatus.MultipleMatches, "Multiple listings matched — an exchange (MIC) must be chosen explicitly.", matches),
        };

        return Task.FromResult(result);
    }

    private static InstrumentResolutionResult Invalid(string error) =>
        new(InstrumentResolutionStatus.Invalid, error, []);
}
