namespace FiapOficina.Contracts;

// Events (Pub/Sub)
public record OrderOpened(Guid OrderId, string CustomerName, string VehiclePlate, decimal EstimatedValue);
public record BudgetCreated(Guid OrderId, Guid BudgetId, decimal TotalAmount);
public record BudgetApproved(Guid OrderId, Guid BudgetId, decimal Amount);
public record PaymentProcessed(Guid OrderId, Guid PaymentId, bool Success, string Message);
public record ExecutionStarted(Guid OrderId);
public record ExecutionFinished(Guid OrderId);
public record OrderFinalized(Guid OrderId);

// Commands (Point-to-Point from SAGA)
public record CreateBudgetCommand(Guid OrderId, decimal EstimatedValue);
public record ProcessPaymentCommand(Guid OrderId, Guid BudgetId, decimal Amount);
public record StartExecutionCommand(Guid OrderId);
