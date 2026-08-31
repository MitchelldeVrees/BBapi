using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dma.OrderIntake.Infrastructure.BackgroundProcessing;

// This is the "background worker" from the reliability design: it — never
// the HTTP request that submitted the order — is what actually calls out to
// EMSX. A failure here (thrown exception or a rejection result) marks the
// order StagingFailed and the message processed — terminal for now, not
// retried (see ProcessMessageAsync). It can never cause a second order to get
// created, because SubmitOrderHandler only ever runs Submit() once per order,
// regardless of how many times this worker (or a retry policy, later) looks
// at the resulting outbox message.
public class OutboxProcessorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox processing loop failed unexpectedly.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var emsxGateway = scope.ServiceProvider.GetRequiredService<IEmsxStagingGateway>();
        var auditTrail = scope.ServiceProvider.GetRequiredService<IOrderAuditTrail>();

        var messages = await outboxRepository.GetUnprocessedAsync(BatchSize, cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, orderRepository, outboxRepository, emsxGateway, auditTrail, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IOrderRepository orderRepository,
        IOutboxRepository outboxRepository,
        IEmsxStagingGateway emsxGateway,
        IOrderAuditTrail auditTrail,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(message.OrderId, cancellationToken);
        if (order is null)
        {
            // Nothing sane to update — leave the message for inspection rather
            // than silently dropping it.
            logger.LogWarning("Outbox message {MessageId} references missing order {OrderId}.", message.Id, message.OrderId);
            return;
        }

        message.RecordAttemptStarted();

        try
        {
            order.BeginStaging();
            await auditTrail.RecordAsync(
                OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.StagingStarted, "EMSX staging started", order.CorrelationId),
                cancellationToken);

            var command = new StageOrderCommand(
                order.Id,
                order.InternalOrderNumber,
                order.Figi ?? throw new InvalidOperationException("Order has no confirmed FIGI — it should never have reached Submitted."),
                order.Side,
                order.Quantity,
                order.OrderType,
                order.LimitPrice,
                order.AccountCode);

            var result = await emsxGateway.StageOrderAsync(command, cancellationToken);

            if (result.Success && result.SequenceNumber is not null)
            {
                order.MarkStagedInEmsx(result.SequenceNumber);
                message.MarkProcessed();
                logger.LogInformation(
                    "[SIMULATION] Order {InternalOrderNumber} staged in mock EMSX as {SequenceNumber}.",
                    order.InternalOrderNumber, result.SequenceNumber);
                await auditTrail.RecordAsync(
                    OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.StagedInEmsx, "EMSX staging successful", order.CorrelationId, result.SequenceNumber),
                    cancellationToken);
            }
            else
            {
                var error = result.ErrorMessage ?? "EMSX staging failed with no error detail.";
                order.MarkStagingFailed(error);
                message.RecordFailure(error);
                // Terminal for now: a real retry policy (backoff, max attempts,
                // distinguishing transient vs. permanent rejections) is future
                // work. What matters here is that this can never turn into a
                // second Submit — see SubmitOrderHandler's idempotency check.
                message.MarkProcessed();
                await auditTrail.RecordAsync(
                    OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.StagingFailed, $"EMSX staging failed: {error}", order.CorrelationId, error),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // A thrown exception (network drop, timeout, ...) is exactly the
            // case the outbox pattern protects against: the order is marked
            // StagingFailed for visibility. Same "terminal for now" reasoning
            // as above — it is never silently turned into a second Submit.
            order.MarkStagingFailed(ex.Message);
            message.RecordFailure(ex.Message);
            message.MarkProcessed();
            logger.LogError(ex, "Staging order {OrderId} in mock EMSX failed.", order.Id);
            await auditTrail.RecordAsync(
                OrderAuditEvent.Record(order.Id, OrderAuditEventTypes.StagingFailed, $"EMSX staging failed: {ex.Message}", order.CorrelationId, ex.Message),
                cancellationToken);
        }

        await outboxRepository.SaveProcessingResultAsync(order, message, cancellationToken);
    }
}
