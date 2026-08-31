// Mirrors Dma.OrderIntake.Contracts.CustomerAccount on the backend, which is
// currently served by MockDmaConnectClient standing in for dmaConnect.
export interface CustomerAccount {
  customerId: string;
  customerName: string;
  accountId: string;
  accountCode: string;
  currency: string;
}
