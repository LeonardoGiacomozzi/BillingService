using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using FiapOficina.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace FiapOficina.BillingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly ILogger<BillingController> _logger;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _repository;
    private readonly IBus _bus;

    public BillingController(
        ILogger<BillingController> logger,
        IPaymentService paymentService,
        IPaymentRepository repository,
        IBus bus)
    {
        _logger = logger;
        _paymentService = paymentService;
        _repository = repository;
        _bus = bus;
    }

    [HttpPost("budgets/approve")]
    public async Task<IActionResult> ApproveBudget([FromBody] BudgetApproved request)
    {
        _logger.LogInformation("SAGA: Aprovando orçamento para ordem {OrderId}", request.OrderId);
        
        // Em um sistema real, validaríamos se o orçamento existe no DynamoDB
        
        // Publicar evento de aprovação para continuar a SAGA
        await _bus.Publish(request);
        
        return Ok(new { Message = "Orçamento aprovado com sucesso. Processando pagamento...", request.OrderId });
    }

    [HttpPost("payments")]
    public async Task<IActionResult> ProcessPayment([FromBody] Payment payment)
    {
        _logger.LogInformation("Processando pagamento manual para orçamento {BudgetId}", payment.BudgetId);
        
        var result = await _paymentService.CreatePixPaymentAsync(payment.OrderId, payment.Value);
        
        payment.Status = result.Status ?? "Pending";
        payment.TransactionId = result.PaymentId;
        
        await _repository.SaveAsync(payment);
        
        return Ok(new
        {
            payment.OrderId,
            payment.Status,
            payment.TransactionId,
            result.QrCode,
            result.QrCodeBase64,
            result.Message
        });
    }

    [HttpGet("payments/{orderId}")]
    public async Task<IActionResult> GetPaymentByOrderId(Guid orderId)
    {
        var payment = await _repository.GetByOrderIdAsync(orderId);
        if (payment == null)
        {
            return NotFound(new { Message = "Pagamento não encontrado para esta ordem" });
        }
        
        return Ok(payment);
    }
}
