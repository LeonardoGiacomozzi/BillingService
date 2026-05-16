using FiapOficina.BillingService.Api.Controllers;
using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using FiapOficina.Contracts;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;

namespace FiapOficina.BillingService.Tests;

public class BillingControllerTests
{
    private readonly Mock<ILogger<BillingController>> _loggerMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IAmazonDynamoDB> _dynamoDbMock;
    private readonly Mock<IPaymentRepository> _repositoryMock;
    private readonly Mock<IBus> _busMock;
    private readonly BillingController _controller;

    public BillingControllerTests()
    {
        _loggerMock = new Mock<ILogger<BillingController>>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _repositoryMock = new Mock<IPaymentRepository>();
        _busMock = new Mock<IBus>();
        _controller = new BillingController(_loggerMock.Object, _paymentServiceMock.Object, _repositoryMock.Object, _busMock.Object);
    }

    [Fact]
    public async Task ApproveBudget_ShouldReturnOk_AndPublishEvent()
    {
        // Arrange
        var request = new BudgetApproved(Guid.NewGuid(), Guid.NewGuid(), 500);

        // Act
        var result = await _controller.ApproveBudget(request);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        _busMock.Verify(b => b.Publish(It.IsAny<BudgetApproved>(), default), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_ShouldReturnPaidStatus_WhenRequestIsValid()
    {
        // Arrange
        var payment = new Payment { BudgetId = Guid.NewGuid(), OrderId = Guid.NewGuid(), Value = 500 };
        
        _paymentServiceMock.Setup(s => s.CreatePixPaymentAsync(payment.OrderId, payment.Value))
            .ReturnsAsync(new PixPaymentResult 
            { 
                PaymentId = "MP-12345", 
                Status = "Paid" 
            });

        // Act
        var result = await _controller.ProcessPayment(payment);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetByOrderId_ShouldReturnNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Payment)null!);
        
        // Act
        var result = await _controller.GetPaymentByOrderId(orderId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
