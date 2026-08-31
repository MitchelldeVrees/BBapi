// Mirrors Dma.OrderIntake.Contracts.OrderDto on the backend.
export interface OrderDto {
  id: string;
  internalOrderNumber: string;
  correlationId: string;
  customerId: string;
  customerName: string;
  accountId: string;
  accountCode: string;
  instrumentId: string | null;
  figi: string | null;
  instrumentName: string | null;
  side: string;
  quantity: number;
  orderType: string;
  limitPrice: number | null;
  currency: string;
  customerReference: string | null;
  notes: string | null;
  status: string;
  createdAtUtc: string;
  emsxSequenceNumber: string | null;
  stagingFailureReason: string | null;
}

// Mirrors Dma.OrderIntake.Contracts.CreateOrderRequest.
export interface CreateOrderRequest {
  customerId: string;
  customerName: string;
  accountId: string;
  accountCode: string;
  side: string;
  quantity: number;
  orderType: string;
  limitPrice: number | null;
  currency: string;
  customerReference?: string | null;
  notes?: string | null;
}
