using MercadoPago.Client;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;

namespace FiapOficina.BillingService.Api.Services;

public interface IMercadoPagoClientWrapper
{
    Task<Payment> CreateAsync(PaymentCreateRequest request, RequestOptions requestOptions = null, CancellationToken cancellationToken = default);
    Task<Payment> GetAsync(long id, RequestOptions requestOptions = null, CancellationToken cancellationToken = default);
}

public class MercadoPagoClientWrapper : IMercadoPagoClientWrapper
{
    private readonly PaymentClient _client;

    public MercadoPagoClientWrapper()
    {
        _client = new PaymentClient();
    }

    public async Task<Payment> CreateAsync(PaymentCreateRequest request, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
    {
        return await _client.CreateAsync(request, requestOptions, cancellationToken);
    }

    public async Task<Payment> GetAsync(long id, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync(id, requestOptions, cancellationToken);
    }
}
