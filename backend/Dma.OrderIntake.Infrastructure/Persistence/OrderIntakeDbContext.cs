using Dma.OrderIntake.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dma.OrderIntake.Infrastructure.Persistence;

public class OrderIntakeDbContext(DbContextOptions<OrderIntakeDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<OrderAuditEvent> OrderAuditEvents => Set<OrderAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.InternalOrderNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(o => o.InternalOrderNumber).IsUnique();
            entity.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(o => o.AccountCode).HasMaxLength(50).IsRequired();
            entity.Property(o => o.Quantity).HasPrecision(18, 8);
            entity.Property(o => o.LimitPrice).HasPrecision(18, 8);
            entity.Property(o => o.Currency).HasMaxLength(3);
            entity.Property(o => o.Figi).HasMaxLength(12);
            entity.Property(o => o.InstrumentName).HasMaxLength(200);
            entity.Property(o => o.EmsxSequenceNumber).HasMaxLength(50);
            entity.Property(o => o.IdempotencyKey).HasMaxLength(100);
            // Partial index: many orders are still Draft (IdempotencyKey is
            // null) and SQLite/EF's unique index otherwise treats every NULL
            // as distinct anyway, but being explicit here is clearer intent —
            // this is a defense-in-depth check, not a substitute for
            // SubmitOrderHandler's own key comparison.
            entity.HasIndex(o => o.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Type).HasMaxLength(50).IsRequired();
            // Not a foreign key on purpose: the outbox is deliberately decoupled
            // from the Orders table's own lifecycle/cascade behaviour.
            entity.Property(m => m.OrderId).IsRequired();
            entity.HasIndex(m => m.ProcessedAtUtc);
        });

        modelBuilder.Entity<OrderAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ActorUser).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Reference).HasMaxLength(200);
            // Not a foreign key, same reasoning as OutboxMessage: append-only
            // audit log, deliberately decoupled from Orders' own lifecycle.
            entity.HasIndex(e => e.OrderId);
        });
    }
}
