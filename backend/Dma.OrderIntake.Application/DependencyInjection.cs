using Dma.OrderIntake.Application.UseCases.AdminEmsxMockScenario;
using Dma.OrderIntake.Application.UseCases.ConfirmInstrument;
using Dma.OrderIntake.Application.UseCases.CreateOrder;
using Dma.OrderIntake.Application.UseCases.GetAccounts;
using Dma.OrderIntake.Application.UseCases.GetOrderAuditTrail;
using Dma.OrderIntake.Application.UseCases.GetOrderById;
using Dma.OrderIntake.Application.UseCases.GetOrders;
using Dma.OrderIntake.Application.UseCases.ResolveInstrument;
using Dma.OrderIntake.Application.UseCases.SubmitOrder;
using Microsoft.Extensions.DependencyInjection;

namespace Dma.OrderIntake.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetOrdersHandler, GetOrdersHandler>();
        services.AddScoped<IGetOrderByIdHandler, GetOrderByIdHandler>();
        services.AddScoped<ICreateOrderHandler, CreateOrderHandler>();
        services.AddScoped<IGetAccountsHandler, GetAccountsHandler>();
        services.AddScoped<IResolveInstrumentHandler, ResolveInstrumentHandler>();
        services.AddScoped<IConfirmInstrumentHandler, ConfirmInstrumentHandler>();
        services.AddScoped<ISubmitOrderHandler, SubmitOrderHandler>();
        services.AddScoped<IGetEmsxMockScenarioHandler, GetEmsxMockScenarioHandler>();
        services.AddScoped<ISetEmsxMockScenarioHandler, SetEmsxMockScenarioHandler>();
        services.AddScoped<IGetOrderAuditTrailHandler, GetOrderAuditTrailHandler>();

        return services;
    }
}
