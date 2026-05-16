using FiapOficina.Contracts;
using FiapOficina.BillingService.Api.Consumers;
using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using Amazon.DynamoDBv2;

namespace FiapOficina.BillingService.Tests;

public class BillingConsumersTests
{
    private readonly Mock<ILogger<BudgetApprovedConsumer>> _loggerMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IAmazonDynamoDB> _dynamoDbMock;
    private readonly Mock<IPaymentRepository> _repositoryMock;

    public BillingConsumersTests()
    {
        _loggerMock = new Mock<ILogger<BudgetApprovedConsumer>>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _repositoryMock = new Mock<IPaymentRepository>();
    }

    [Fact]
    public async Task BudgetApprovedConsumer_ShouldProcessPayment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var consumer = new BudgetApprovedConsumer(_loggerMock.Object, _paymentServiceMock.Object, _repositoryMock.Object);
        
        var contextMock = new Mock<ConsumeContext<BudgetApproved>>();
        contextMock.Setup(c => c.Message).Returns(new BudgetApproved(orderId, budgetId, 500));
        
        _paymentServiceMock.Setup(s => s.CreatePixPaymentAsync(orderId, 500))
            .ReturnsAsync(new PixPaymentResult { Status = "Pending", PaymentId = "123" });

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _paymentServiceMock.Verify(s => s.CreatePixPaymentAsync(orderId, 500), Times.Once);
    }
}
