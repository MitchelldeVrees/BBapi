using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Infrastructure.BackgroundProcessing;
using Dma.OrderIntake.Infrastructure.Configuration;
using Dma.OrderIntake.Infrastructure.DmaConnect;
using Dma.OrderIntake.Infrastructure.Emsx;
using Dma.OrderIntake.Infrastructure.InstrumentResolution;
using Dma.OrderIntake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dma.OrderIntake.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderIntakeDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("OrderIntakeDb")));

        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IOutboxRepository, EfOutboxRepository>();
        services.AddScoped<IOrderAuditTrail, EfOrderAuditTrail>();

        var orderIntakeOptions =
            configuration.GetSection(OrderIntakeInfrastructureOptions.SectionName).Get<OrderIntakeInfrastructureOptions>()
            ?? new OrderIntakeInfrastructureOptions();
        services.AddSingleton(orderIntakeOptions.Emsx);

        // Mock stands in until the real dmaConnect integration exists. Stateless
        // demo data, so a singleton is fine.
        services.AddSingleton<IDmaConnectClient, MockDmaConnectClient>();

        // InstrumentResolver: "Mock" | "OpenFigi" — see OrderIntakeInfrastructureOptions.
        if (string.Equals(orderIntakeOptions.InstrumentResolver, "OpenFigi", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IInstrumentResolver, OpenFigiInstrumentResolver>(client =>
            {
                client.BaseAddress = new Uri("https://api.openfigi.com/v3/");
            });
        }
        else
        {
            services.AddSingleton<IInstrumentResolver, MockInstrumentResolver>();
        }

        // Emsx:Environment: "Mock" | "Beta" | "Production" — see
        // OrderIntakeInfrastructureOptions. The admin mock-scenario store is
        // always registered (harmless if unused) so the admin endpoints never
        // fail to resolve regardless of which gateway is active.
        services.AddSingleton<IEmsxMockScenarioStore, EmsxMockScenarioStore>();
        if (string.Equals(orderIntakeOptions.Emsx.Environment, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmsxStagingGateway, MockEmsxStagingGateway>();
        }
        else
        {
            // SIMULATION-ONLY today regardless: this is a compilable skeleton,
            // not a working Bloomberg integration — see BloombergEmsxStagingGateway.
            services.AddSingleton<IEmsxStagingGateway, BloombergEmsxStagingGateway>();
        }

        // Polls the outbox and drives the (mocked, for now always) EMSX call.
        // Never runs inline with a customer's HTTP request — see SubmitOrderHandler.
        services.AddHostedService<OutboxProcessorWorker>();

        return services;
    }

    // Keeps EF Core specifics out of the Api project's Program.cs — the Api
    // just calls this once at startup.
    public static async Task MigrateInfrastructureAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderIntakeDbContext>();
        await db.Database.MigrateAsync();
    }
}
