// Mirrors Dma.OrderIntake.Contracts.AuditEventDto on the backend.
export interface AuditEventDto {
  eventType: string;
  description: string;
  occurredAtUtc: string;
  actorUser: string;
  correlationId: string;
  reference: string | null;
}
