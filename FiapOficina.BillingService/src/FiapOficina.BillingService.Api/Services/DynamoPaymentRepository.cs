using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FiapOficina.BillingService.Api.Models;

namespace FiapOficina.BillingService.Api.Services;

public class DynamoPaymentRepository
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public DynamoPaymentRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        _context = new DynamoDBContext(dynamoDb);
    }

    public async Task SaveAsync(Payment payment)
    {
        await _context.SaveAsync(payment);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.LoadAsync<Payment>(orderId);
    }

    public async Task<Payment?> GetByPaymentIdAsync(string paymentId)
    {
        var search = _context.FromQueryAsync<Payment>(new QueryOperationConfig
        {
            IndexName = "TransactionIdIndex",
            Filter = new QueryFilter("TransactionId", QueryOperator.Equal, paymentId)
        });

        var results = await search.GetRemainingAsync();
        return results.FirstOrDefault();
    }
}
