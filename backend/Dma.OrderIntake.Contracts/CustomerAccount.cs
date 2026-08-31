namespace Dma.OrderIntake.Contracts;

// The shape dmaConnect will eventually return for an account (custodian
// mapping, settlement context, etc. are later additions — kept minimal here).
// Used directly as the API's wire shape too, no separate DTO needed.
public record CustomerAccount(
    Guid CustomerId,
    string CustomerName,
    Guid AccountId,
    string AccountCode,
    string Currency);
