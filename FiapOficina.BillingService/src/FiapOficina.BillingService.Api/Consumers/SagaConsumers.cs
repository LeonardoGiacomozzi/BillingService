using FiapOficina.Contracts;
using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using MassTransit;

namespace FiapOficina.BillingService.Api.Consumers;

public class BudgetApprovedConsumer : IConsumer<BudgetApproved>
{
    private readonly ILogger<BudgetApprovedConsumer> _logger;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _repository;

    public BudgetApprovedConsumer(
        ILogger<BudgetApprovedConsumer> logger, 
        IPaymentService paymentService,
        IPaymentRepository repository)
    {
        _logger = logger;
        _paymentService = paymentService;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<BudgetApproved> context)
    {
        _logger.LogInformation("SAGA: Orçamento Aprovado. Processando pagamento de {Amount} para Ordem {OrderId}", context.Message.Amount, context.Message.OrderId);
        
        // Criar pagamento PIX no Mercado Pago
        var result = await _paymentService.CreatePixPaymentAsync(context.Message.OrderId, context.Message.Amount);
        
        // Salvar estado do pagamento
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BudgetId = context.Message.BudgetId,
            OrderId = context.Message.OrderId,
            Value = context.Message.Amount,
            Status = result.Status ?? "Pending",
            TransactionId = result.PaymentId
        };
        
        await _repository.SaveAsync(payment);
        
        _logger.LogInformation("SAGA: Pagamento PIX gerado para Ordem {OrderId}. Status: {Status}", context.Message.OrderId, result.Status);
        
        if (result.QrCode != null)
        {
            _logger.LogInformation("PIX Copia e Cola: {QrCode}", result.QrCode);
        }

        // Se der erro na geracao do PIX, falha
        if (result.Status == "Failed")
        {
            await context.Publish(new PaymentProcessed(context.Message.OrderId, payment.Id, false, result.Message));
        }
    }
}
