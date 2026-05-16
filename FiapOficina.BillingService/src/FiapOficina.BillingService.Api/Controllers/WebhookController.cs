using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using FiapOficina.Contracts;
using MassTransit;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.Mvc;

namespace FiapOficina.BillingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly DynamoPaymentRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMercadoPagoClientWrapper _mercadoPagoClient;

    public WebhookController(
        ILogger<WebhookController> logger,
        DynamoPaymentRepository repository,
        IPublishEndpoint publishEndpoint,
        IMercadoPagoClientWrapper mercadoPagoClient)
    {
        _logger = logger;
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _mercadoPagoClient = mercadoPagoClient;
    }

    [HttpPost("mercadopago")]
    public async Task<IActionResult> HandleMercadoPagoNotification([FromBody] MercadoPagoWebhook payload)
    {
        _logger.LogInformation("Recebida notificação do Mercado Pago: Type={Type}, Action={Action}", payload.Type, payload.Action);

        if (payload.Type == "payment" && payload.Data?.Id != null)
        {
            try
            {
                var paymentId = long.Parse(payload.Data.Id);
                MercadoPago.Resource.Payment.Payment payment = await _mercadoPagoClient.GetAsync(paymentId);

                _logger.LogInformation("Detalhes do pagamento {PaymentId} recuperados. Status: {Status}", paymentId, payment.Status);

                if (payment.Status == "approved")
                {
                    if (Guid.TryParse(payment.ExternalReference, out Guid orderId))
                    {
                        var localPayment = await _repository.GetByOrderIdAsync(orderId);
                        if (localPayment != null)
                        {
                            localPayment.Status = "Paid";
                            await _repository.SaveAsync(localPayment);

                            _logger.LogInformation("Pagamento aprovado para Ordem {OrderId}. Publicando evento.", orderId);

                            await _publishEndpoint.Publish(new PaymentProcessed(
                                orderId,
                                localPayment.Id,
                                true,
                                "Pagamento aprovado via Mercado Pago Webhook"
                            ));
                        }
                        else
                        {
                            _logger.LogWarning("Ordem {OrderId} não encontrada no repositório local.", orderId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("ExternalReference '{ExternalReference}' não é um Guid válido.", payment.ExternalReference);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar notificação do Mercado Pago para ID {Id}", payload.Data.Id);
            }
        }

        // Mercado Pago exige resposta 200 OK ou 201 Created
        return Ok();
    }
}

public class MercadoPagoWebhook
{
    public string? Action { get; set; }
    public string? Type { get; set; }
    public MercadoPagoWebhookData? Data { get; set; }
}

public class MercadoPagoWebhookData
{
    public string? Id { get; set; }
}
