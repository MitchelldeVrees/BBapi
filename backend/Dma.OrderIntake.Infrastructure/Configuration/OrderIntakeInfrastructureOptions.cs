namespace Dma.OrderIntake.Infrastructure.Configuration;

// Binds the "OrderIntake" appsettings.json section. This is what picks Mock
// vs. the real adapter for each integration — see AddInfrastructure.
//
//   "OrderIntake": {
//     "InstrumentResolver": "Mock",       // "Mock" | "OpenFigi"
//     "Emsx": { "Environment": "Mock" }   // "Mock" | "Beta" | "Production"
//   }
public class OrderIntakeInfrastructureOptions
{
    public const string SectionName = "OrderIntake";

    public string InstrumentResolver { get; set; } = "Mock";

    public EmsxOptions Emsx { get; set; } = new();
}

public class EmsxOptions
{
    public string Environment { get; set; } = "Mock";
}
