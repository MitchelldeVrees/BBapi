using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Infrastructure.Emsx;

// In-memory by design: this is admin test/demo configuration, not something
// that needs to survive a restart. Registered as a singleton, read by
// MockEmsxStagingGateway and written by the admin endpoints — both can run
// concurrently (a live worker poll vs. an admin Apply click), hence the lock.
public class EmsxMockScenarioStore : IEmsxMockScenarioStore
{
    private readonly object gate = new();
    private EmsxMockScenarioSettings current = new(EmsxMockScenario.Success, 0);

    public EmsxMockScenarioSettings GetCurrent()
    {
        lock (gate)
        {
            return current;
        }
    }

    public void Set(EmsxMockScenarioSettings settings)
    {
        lock (gate)
        {
            current = settings with { ArtificialDelayMs = Math.Max(0, settings.ArtificialDelayMs) };
        }
    }
}
