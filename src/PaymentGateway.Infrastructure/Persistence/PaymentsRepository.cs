using System.Collections.Concurrent;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory store. Registered as a singleton, so data lives for the lifetime of the process.
/// </summary>
public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public void Add(Payment payment)
    {
        if (!_payments.TryAdd(payment.Id, payment))
        {
            throw new InvalidOperationException($"A payment with id {payment.Id} already exists.");
        }
    }

    public Payment? Get(Guid id) => _payments.GetValueOrDefault(id);
}
