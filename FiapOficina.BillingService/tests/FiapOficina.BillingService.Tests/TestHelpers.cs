using FiapOficina.BillingService.Api.Models;
using FiapOficina.BillingService.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiapOficina.BillingService.Tests;

public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _payments = new();

    public Task SaveAsync(Payment payment)
    {
        var existing = _payments.FirstOrDefault(p => p.OrderId == payment.OrderId);
        if (existing != null) _payments.Remove(existing);
        _payments.Add(payment);
        return Task.CompletedTask;
    }

    public void Save(Payment payment) => SaveAsync(payment).Wait();

    public Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return Task.FromResult(_payments.FirstOrDefault(p => p.OrderId == orderId));
    }

    public Task<Payment?> GetByPaymentIdAsync(string paymentId)
    {
        return Task.FromResult(_payments.FirstOrDefault(p => p.TransactionId == paymentId));
    }

    public Payment? GetByOrderId(Guid orderId) => GetByOrderIdAsync(orderId).Result;
}
