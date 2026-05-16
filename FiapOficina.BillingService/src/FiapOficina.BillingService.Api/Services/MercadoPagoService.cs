using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using Microsoft.Extensions.Configuration;

namespace FiapOficina.BillingService.Api.Services;

public class MercadoPagoService : IPaymentService
{
    private readonly ILogger<MercadoPagoService> _logger;
    private readonly IMercadoPagoClientWrapper _client;

    public MercadoPagoService(ILogger<MercadoPagoService> logger, IMercadoPagoClientWrapper client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<PixPaymentResult> CreatePixPaymentAsync(Guid orderId, decimal amount)
    {
        _logger.LogInformation("Criando pagamento PIX para Ordem {OrderId} no valor de {Amount}", orderId, amount);

        try
        {
            var paymentCreateRequest = new PaymentCreateRequest
            {
                TransactionAmount = amount,
                Description = $"Pagamento Ordem de Serviço {orderId}",
                PaymentMethodId = "pix",
                ExternalReference = orderId.ToString(),
                Payer = new PaymentPayerRequest
                {
                    Email = "cliente.oficina@teste.com", // Dummy email as required by PIX
                    FirstName = "Cliente",
                    LastName = "Oficina",
                    Identification = new IdentificationRequest
                    {
                        Type = "CPF",
                        Number = "12345678909" // Dummy CPF
                    }
                }
            };

            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("X-Idempotency-Key", orderId.ToString()); // Use OrderId as idempotency key

            Payment payment = await _client.CreateAsync(paymentCreateRequest, requestOptions);

            _logger.LogInformation("Pagamento PIX criado com sucesso. ID: {PaymentId}", payment.Id);

            return new PixPaymentResult
            {
                PaymentId = payment.Id.ToString(),
                Status = payment.Status,
                QrCode = payment.PointOfInteraction?.TransactionData?.QrCode,
                QrCodeBase64 = payment.PointOfInteraction?.TransactionData?.QrCodeBase64,
                Message = "PIX gerado com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar pagamento PIX para Ordem {OrderId}", orderId);
            return new PixPaymentResult
            {
                Status = "Failed",
                Message = $"Erro ao gerar PIX: {ex.Message}"
            };
        }
    }
}
