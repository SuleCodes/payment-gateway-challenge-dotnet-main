using PaymentGateway.Application.Abstractions;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Infrastructure.Persistence;

public class PaymentsRepository : IPaymentsRepository
{
    public List<Payment> Payments = new();
    
    public void Add(Payment payment)
    {
        Payments.Add(payment);
    }

    public Payment? Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}
