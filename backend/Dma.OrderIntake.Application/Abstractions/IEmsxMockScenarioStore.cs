using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.Abstractions;

// Backs the admin-only mock-scenario panel. Read by MockEmsxStagingGateway on
// every staging attempt. In-memory by design — this is test/demo
// configuration, not something that needs to survive a restart.
public interface IEmsxMockScenarioStore
{
    EmsxMockScenarioSettings GetCurrent();

    void Set(EmsxMockScenarioSettings settings);
}
