using Dma.OrderIntake.Application;
using Dma.OrderIntake.Application.UseCases.AdminEmsxMockScenario;
using Dma.OrderIntake.Application.UseCases.ConfirmInstrument;
using Dma.OrderIntake.Application.UseCases.CreateOrder;
using Dma.OrderIntake.Application.UseCases.GetAccounts;
using Dma.OrderIntake.Application.UseCases.GetOrderAuditTrail;
using Dma.OrderIntake.Application.UseCases.GetOrderById;
using Dma.OrderIntake.Application.UseCases.GetOrders;
using Dma.OrderIntake.Application.UseCases.ResolveInstrument;
using Dma.OrderIntake.Application.UseCases.SubmitOrder;
using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;
using Dma.OrderIntake.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "DMA Order Intake API",
        Version = "v1"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Api is the composition root: it wires Application + Infrastructure together.
// It never talks to EF Core, SQLite, or the Order entity directly.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply pending EF Core migrations on startup so the SQLite database is always up to date.
await app.Services.MigrateInfrastructureAsync();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendDev");

// Domain rules (Order.Create, Order.Submit, ...) are the final authority — this
// is what turns a violated rule into an HTTP response. The rule itself is never
// only enforced client-side.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

var orders = app.MapGroup("/api/order-intake/orders");

orders.MapPost("/", async (CreateOrderRequest request, ICreateOrderHandler handler, CancellationToken cancellationToken) =>
{
    var order = await handler.HandleAsync(request, cancellationToken);
    return Results.Created($"/api/order-intake/orders/{order.Id}", order);
})
.WithName("CreateOrder");

orders.MapGet("/", async (IGetOrdersHandler handler, CancellationToken cancellationToken) =>
    await handler.HandleAsync(cancellationToken))
.WithName("GetOrders");

orders.MapGet("/{id:guid}", async (Guid id, IGetOrderByIdHandler handler, CancellationToken cancellationToken) =>
    await handler.HandleAsync(id, cancellationToken) is { } order
        ? Results.Ok(order)
        : Results.NotFound())
.WithName("GetOrderById");

// The explicit "confirm" step — separate from /instruments/resolve on purpose.
// Resolving never sets Order.InstrumentId by itself.
orders.MapPost("/{id:guid}/confirm-instrument", async (
    Guid id, ConfirmInstrumentRequest request, IConfirmInstrumentHandler handler, CancellationToken cancellationToken) =>
    await handler.HandleAsync(id, request, cancellationToken) is { } order
        ? Results.Ok(order)
        : Results.NotFound())
.WithName("ConfirmInstrument");

orders.MapGet("/{id:guid}/audit-trail", async (Guid id, IGetOrderAuditTrailHandler handler, CancellationToken cancellationToken) =>
    await handler.HandleAsync(id, cancellationToken))
.WithName("GetOrderAuditTrail");

// SIMULATION — NO REAL ORDERS. Returns 202: this only ever commits the order's
// status change + an outbox message in one transaction. The actual (mocked)
// EMSX staging call happens later, out of band, in OutboxProcessorWorker —
// never inline with this request. See docs/architecture.md.
//
// Idempotency-Key is required: if the exact same key is sent twice (Angular
// retrying, a flaky network, a double click that slipped past the disabled
// button), the second call returns the order's current state instead of
// running Submit() again — see SubmitOrderHandler.
orders.MapPost("/{id:guid}/submit", async (
    Guid id,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    ISubmitOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Results.BadRequest(new { error = "Idempotency-Key header is required." });
    }

    var order = await handler.HandleAsync(id, idempotencyKey, cancellationToken);
    return order is not null
        ? Results.Accepted($"/api/order-intake/orders/{order.Id}", order)
        : Results.NotFound();
})
.WithName("SubmitOrder");

// Backed by MockDmaConnectClient for now — same shape the real dmaConnect
// integration will return later, so nothing downstream has to change.
app.MapGet("/api/order-intake/accounts", async (IGetAccountsHandler handler, CancellationToken cancellationToken) =>
    await handler.HandleAsync(cancellationToken))
.WithName("GetAccounts");

// Backed by MockInstrumentResolver for now. This only ever looks something
// up — it never attaches an instrument to an order. See confirm-instrument.
app.MapPost("/api/order-intake/instruments/resolve", async (
    InstrumentResolutionRequest request, IResolveInstrumentHandler handler, CancellationToken cancellationToken) =>
    await handler.HandleAsync(request, cancellationToken))
.WithName("ResolveInstrument");

// ADMIN — testing/ops tooling for the mock EMSX gateway only; none of this
// exists once a real integration replaces MockEmsxStagingGateway. No real
// access control yet (IdentityServer is out of scope so far) — the route
// prefix and the frontend's own banner are the only things marking this as
// admin-only today.
var admin = app.MapGroup("/api/order-intake/admin");

admin.MapGet("/emsx-mock-scenario", (IGetEmsxMockScenarioHandler handler) => handler.Handle())
    .WithName("GetEmsxMockScenario");

admin.MapPost("/emsx-mock-scenario", (EmsxMockScenarioSettings settings, ISetEmsxMockScenarioHandler handler) => handler.Handle(settings))
    .WithName("SetEmsxMockScenario");

app.Run();
