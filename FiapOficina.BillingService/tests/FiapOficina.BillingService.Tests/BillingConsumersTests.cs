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
    public async Task BudgetApprovedConsumer_ShouldProcessPayment_WhenPending()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var consumer = new BudgetApprovedConsumer(_loggerMock.Object, _paymentServiceMock.Object, _repositoryMock.Object);
        
        var contextMock = new Mock<ConsumeContext<BudgetApproved>>();
        contextMock.Setup(c => c.Message).Returns(new BudgetApproved(orderId, budgetId, 500));
        
        _paymentServiceMock.Setup(s => s.CreatePixPaymentAsync(orderId, 500))
            .ReturnsAsync(new PixPaymentResult { Status = "Pending", PaymentId = "123", QrCode = "PIX_QR_CODE" });

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _paymentServiceMock.Verify(s => s.CreatePixPaymentAsync(orderId, 500), Times.Once);
        _repositoryMock.Verify(r => r.SaveAsync(It.Is<Payment>(p => p.OrderId == orderId && p.Status == "Pending")), Times.Once);
    }

    [Fact]
    public async Task BudgetApprovedConsumer_ShouldPublishPaymentProcessedFailed_WhenPaymentFails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var consumer = new BudgetApprovedConsumer(_loggerMock.Object, _paymentServiceMock.Object, _repositoryMock.Object);
        
        var contextMock = new Mock<ConsumeContext<BudgetApproved>>();
        contextMock.Setup(c => c.Message).Returns(new BudgetApproved(orderId, budgetId, 500));
        
        _paymentServiceMock.Setup(s => s.CreatePixPaymentAsync(orderId, 500))
            .ReturnsAsync(new PixPaymentResult { Status = "Failed", PaymentId = "123", Message = "Limit Exceeded" });

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        contextMock.Verify(c => c.Publish(It.Is<PaymentProcessed>(p => p.OrderId == orderId && p.Success == false && p.Message == "Limit Exceeded"), default), Times.Once);
    }

    [Fact]
    public async Task OrderOpenedConsumer_ShouldPublishBudgetCreated()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var loggerMock = new Mock<ILogger<OrderOpenedConsumer>>();
        var consumer = new OrderOpenedConsumer(loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<OrderOpened>>();
        contextMock.Setup(c => c.Message).Returns(new OrderOpened(orderId, "John", "ABC-1234", 1500));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        contextMock.Verify(c => c.Publish(It.Is<BudgetCreated>(b => b.OrderId == orderId && b.TotalAmount == 1500), default), Times.Once);
    }
}
