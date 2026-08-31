using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;
using Microsoft.Extensions.Logging;

namespace Dma.OrderIntake.Infrastructure.Emsx;

// SIMULATION — NO REAL ORDERS. Stands in for the real Bloomberg EMSX
// integration (BLPAPI / EMSX SDK). Nothing outside this class ever sees a
// Bloomberg SDK type — only StageOrderCommand / EmsxStageResult.
//
// Scope for Goal 1 is strictly "create the parent order in EMSX": no broker
// route, no trade gets created here.
//
// Behaviour is driven by IEmsxMockScenarioStore (the admin panel) so failure
// scenarios the spec calls for can actually be demonstrated on demand,
// instead of only existing in theory.
public class MockEmsxStagingGateway(
    IEmsxMockScenarioStore scenarioStore,
    ILogger<MockEmsxStagingGateway> logger) : IEmsxStagingGateway
{
    public async Task<EmsxStageResult> StageOrderAsync(StageOrderCommand command, CancellationToken cancellationToken)
    {
        var settings = scenarioStore.GetCurrent();

        logger.LogInformation(
            "[SIMULATION] [NO REAL ORDERS] Staging parent order in mock EMSX (scenario={Scenario}, delay={DelayMs}ms): {OrderReferenceId} {Side} {Quantity} {Figi} @ {Account}.",
            settings.Scenario, settings.ArtificialDelayMs, command.OrderReferenceId, command.Side, command.Quantity, command.Figi, command.Account);

        if (settings.ArtificialDelayMs > 0)
        {
            await Task.Delay(settings.ArtificialDelayMs, cancellationToken);
        }

        return settings.Scenario switch
        {
            EmsxMockScenario.Success or EmsxMockScenario.DelayedSuccess => Success(),

            EmsxMockScenario.BloombergRejection =>
                Rejected("Bloomberg rejected the order: instrument not tradable on this venue."),
            EmsxMockScenario.InvalidInstrument =>
                Rejected("Bloomberg rejected the order: unknown or invalid FIGI."),
            EmsxMockScenario.InvalidAccount =>
                Rejected("Bloomberg rejected the order: account not recognized by EMSX."),
            EmsxMockScenario.DuplicateRequest =>
                Rejected("Bloomberg rejected the order: duplicate order reference already staged."),

            // These two throw rather than return a result — they simulate not
            // getting a response at all, which is exactly the ambiguous case
            // the outbox pattern (not a synchronous retry) is built to handle.
            EmsxMockScenario.TemporaryConnectionFailure =>
                throw new IOException("Temporary connection failure — could not reach EMSX."),
            EmsxMockScenario.TimeoutUnknownState =>
                throw new TimeoutException("Timeout / state unknown — no response received from EMSX."),

            _ => Success(),
        };
    }

    private static EmsxStageResult Success() =>
        new(true, $"SIM-{Random.Shared.Next(100000, 999999)}", null);

    private static EmsxStageResult Rejected(string reason) =>
        new(false, null, reason);
}
