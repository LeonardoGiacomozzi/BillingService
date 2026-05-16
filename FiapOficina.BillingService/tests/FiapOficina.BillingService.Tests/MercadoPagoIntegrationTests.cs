using FiapOficina.BillingService.Api.Controllers;
using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using FiapOficina.Contracts;
using MassTransit;
using MercadoPago.Client;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FiapOficina.BillingService.Tests;

public class MercadoPagoIntegrationTests
{
    private readonly Mock<ILogger<WebhookController>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<IMercadoPagoClientWrapper> _clientWrapperMock;
    private readonly InMemoryPaymentRepository _repository;
    private readonly WebhookController _controller;

    public MercadoPagoIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<WebhookController>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _clientWrapperMock = new Mock<IMercadoPagoClientWrapper>();
        _repository = new InMemoryPaymentRepository();
        
        _controller = new WebhookController(
            _loggerMock.Object, 
            _repository, 
            _publishEndpointMock.Object, 
            _clientWrapperMock.Object);
    }

    [Fact]
    public async Task HandleMercadoPagoNotification_ShouldProcessApprovedPayment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var localPayment = new FiapOficina.BillingService.Api.Models.Payment { BudgetId = Guid.NewGuid(), OrderId = orderId, Value = 100, Status = "Pending" };
        _repository.Save(localPayment);

        var payload = new MercadoPagoWebhook
        {
            Type = "payment",
            Action = "payment.created",
            Data = new MercadoPagoWebhookData { Id = "123456" }
        };

        var mockPaymentResponse = new MercadoPago.Resource.Payment.Payment
        {
            Id = 123456,
            Status = "approved",
            ExternalReference = orderId.ToString()
        };

        _clientWrapperMock
            .Setup(c => c.GetAsync(123456, null, default))
            .ReturnsAsync(mockPaymentResponse);

        // Act
        var result = await _controller.HandleMercadoPagoNotification(payload);

        // Assert
        result.Should().BeOfType<OkResult>();
        
        var updatedPayment = _repository.GetByOrderId(orderId);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Status.Should().Be("Paid");

        _publishEndpointMock.Verify(
            x => x.Publish(It.Is<PaymentProcessed>(p => p.OrderId == orderId && p.Success == true), default), 
            Times.Once);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_ShouldReturnSuccessResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MercadoPagoService>>();
        var orderId = Guid.NewGuid();
        var amount = 150.0m;
        
        var mockPaymentResponse = new MercadoPago.Resource.Payment.Payment
        {
            Id = 987654,
            Status = "pending",
            PointOfInteraction = new PaymentPointOfInteraction
            {
                TransactionData = new PaymentTransactionData
                {
                    QrCode = "qrcode_data",
                    QrCodeBase64 = "base64_data"
                }
            }
        };

        _clientWrapperMock
            .Setup(c => c.CreateAsync(It.IsAny<PaymentCreateRequest>(), It.IsAny<RequestOptions>(), default))
            .ReturnsAsync(mockPaymentResponse);

        var service = new MercadoPagoService(loggerMock.Object, _clientWrapperMock.Object);

        // Act
        var result = await service.CreatePixPaymentAsync(orderId, amount);

        // Assert
        result.Should().NotBeNull();
        result.PaymentId.Should().Be("987654");
        result.Status.Should().Be("pending");
        result.QrCode.Should().Be("qrcode_data");
        result.QrCodeBase64.Should().Be("base64_data");
    }
}
