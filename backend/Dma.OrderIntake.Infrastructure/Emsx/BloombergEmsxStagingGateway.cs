using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Infrastructure.Configuration;

namespace Dma.OrderIntake.Infrastructure.Emsx;

// Compilable adapter boundary — NOT a working Bloomberg integration. The
// official Bloomberg EMSX SDK / BLPAPI is proprietary and isn't available in
// this environment, so no Bloomberg SDK types (BloombergSession,
// BloombergRequest, BloombergElement, ...) appear anywhere in this codebase.
// This class is where they would live, fully contained, once the SDK exists —
// nothing above IEmsxStagingGateway (Application, Api, Angular) would change.
//
// What a real implementation needs, per the spec:
// - A session authenticated against //blp/apiauth (both UAT and Production).
// - The staging service itself: UAT uses //blp/emapisvc_beta, Production uses
//   //blp/emapisvc — see ServiceNameFor below, selected by
//   OrderIntake:Emsx:Environment (see OrderIntakeInfrastructureOptions).
// - Goal 1 scope is strictly CreateOrder (staging the parent order only) —
//   explicitly NOT CreateOrderAndRouteEx or RouteEx. No broker route, no trade
//   gets created by this adapter.
public class BloombergEmsxStagingGateway(EmsxOptions options) : IEmsxStagingGateway
{
    public Task<EmsxStageResult> StageOrderAsync(StageOrderCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            $"BloombergEmsxStagingGateway is a compilable adapter boundary, not a working implementation — " +
            $"the official Bloomberg EMSX SDK isn't available yet. Configured environment: '{options.Environment}' " +
            $"(service: {ServiceNameFor(options.Environment)}). Set OrderIntake:Emsx:Environment back to \"Mock\" to run.");
    }

    private static string ServiceNameFor(string environment) => environment switch
    {
        "Production" => "//blp/emapisvc",
        // Any unrecognized value defaults to UAT, never Production by accident.
        _ => "//blp/emapisvc_beta",
    };
}
