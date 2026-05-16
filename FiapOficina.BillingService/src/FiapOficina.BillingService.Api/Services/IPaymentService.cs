namespace FiapOficina.BillingService.Api.Services;

public interface IPaymentService
{
    Task<PixPaymentResult> CreatePixPaymentAsync(Guid orderId, decimal amount);
}

public class PixPaymentResult
{
    public string? QrCode { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? PaymentId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}
