using FiapOficina.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FiapOficina.BillingService.Api.Consumers;

public class OrderOpenedConsumer : IConsumer<OrderOpened>
{
    private readonly ILogger<OrderOpenedConsumer> _logger;

    public OrderOpenedConsumer(ILogger<OrderOpenedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderOpened> context)
    {
        _logger.LogInformation("SAGA: Recebido OrderOpened para Ordem: {OrderId}. Gerando orçamento...", context.Message.OrderId);
        
        var budgetId = Guid.NewGuid();
        
        await context.Publish(new BudgetCreated(context.Message.OrderId, budgetId, context.Message.EstimatedValue));
    }
}
