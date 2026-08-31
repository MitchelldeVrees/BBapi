namespace Dma.OrderIntake.Domain;

// Thrown when a domain invariant or business rule is violated. The Api layer
// translates this into a 400 response — the rule itself always lives here,
// never only in Angular.
public class DomainException(string message) : Exception(message);
