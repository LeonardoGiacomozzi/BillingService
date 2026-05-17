using FiapOficina.BillingService.Api.Models;
using FluentAssertions;
using Xunit;

namespace FiapOficina.BillingService.Tests;

public class ModelTests
{
    [Fact]
    public void Budget_ShouldStoreDataCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var budget = new Budget
        {
            Id = id,
            OrderId = orderId,
            Amount = 250.50m,
            Status = "Approved",
            CreatedAt = createdAt
        };

        // Assert
        budget.Id.Should().Be(id);
        budget.OrderId.Should().Be(orderId);
        budget.Amount.Should().Be(250.50m);
        budget.Status.Should().Be("Approved");
        budget.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Payment_ShouldStoreDataCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var budgetId = Guid.NewGuid();

        // Act
        var payment = new Payment
        {
            OrderId = orderId,
            Id = id,
            BudgetId = budgetId,
            Value = 500m,
            Status = "Paid",
            TransactionId = "TX12345"
        };

        // Assert
        payment.OrderId.Should().Be(orderId);
        payment.Id.Should().Be(id);
        payment.BudgetId.Should().Be(budgetId);
        payment.Value.Should().Be(500m);
        payment.Status.Should().Be("Paid");
        payment.TransactionId.Should().Be("TX12345");
    }
}
