using Amazon.DynamoDBv2.DataModel;

namespace FiapOficina.BillingService.Api.Models;

[DynamoDBTable("FiapOficinaPayments")]
public class Budget
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[DynamoDBTable("FiapOficinaPayments")]
public class Payment
{
    [DynamoDBHashKey]
    public Guid OrderId { get; set; }
    
    [DynamoDBProperty]
    public Guid Id { get; set; }
    
    [DynamoDBProperty]
    public Guid BudgetId { get; set; }
    
    [DynamoDBProperty]
    public decimal Value { get; set; }
    
    [DynamoDBProperty]
    public string Status { get; set; } = "Pending"; // Pending, Paid, Failed
    
    [DynamoDBProperty]
    public string? TransactionId { get; set; } // External ID (Mercado Pago)
}
