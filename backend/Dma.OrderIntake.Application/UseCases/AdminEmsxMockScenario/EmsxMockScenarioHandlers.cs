using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;

namespace Dma.OrderIntake.Application.UseCases.AdminEmsxMockScenario;

public interface IGetEmsxMockScenarioHandler
{
    EmsxMockScenarioSettings Handle();
}

public class GetEmsxMockScenarioHandler(IEmsxMockScenarioStore store) : IGetEmsxMockScenarioHandler
{
    public EmsxMockScenarioSettings Handle() => store.GetCurrent();
}

public interface ISetEmsxMockScenarioHandler
{
    EmsxMockScenarioSettings Handle(EmsxMockScenarioSettings settings);
}

public class SetEmsxMockScenarioHandler(IEmsxMockScenarioStore store) : ISetEmsxMockScenarioHandler
{
    public EmsxMockScenarioSettings Handle(EmsxMockScenarioSettings settings)
    {
        store.Set(settings);
        return store.GetCurrent();
    }
}
