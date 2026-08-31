namespace Dma.OrderIntake.Contracts;

// No InstrumentId here on purpose: instrument resolution is a separate,
// later use case. An order can be created before its instrument is confirmed,
// it just can't be submitted until then.
//
// CustomerName/AccountCode are snapshotted from data the caller already has
// (via GetAccounts / IDmaConnectClient) rather than looked up again here —
// it's what the order overview/detail screens and EMSX staging need later.
public record CreateOrderRequest(
    Guid CustomerId,
    string CustomerName,
    Guid AccountId,
    string AccountCode,
    string Side,
    decimal Quantity,
    string OrderType,
    decimal? LimitPrice,
    string Currency,
    string? CustomerReference,
    string? Notes);
