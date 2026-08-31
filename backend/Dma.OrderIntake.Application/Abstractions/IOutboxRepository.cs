using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Application.Abstractions;

public interface IOutboxRepository
{
    // Order + OutboxMessage are persisted together in a single transaction —
    // this is the "commit together" step the reliability design depends on.
    // If this fails, neither the status change nor the queued work exist.
    Task EnqueueStagingMessageAsync(Order order, OutboxMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken);

    // Same "commit together" reasoning as above, for the write-back after a
    // processing attempt: the order's new staging status and the outbox
    // message's attempt/result bookkeeping must land atomically.
    Task SaveProcessingResultAsync(Order order, OutboxMessage message, CancellationToken cancellationToken);
}
