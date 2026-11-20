using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Abstractions;

/// <summary>
/// Persistence port. Defined by the application layer, implemented in infrastructure.
/// </summary>
public interface IPaymentsRepository
{
    void Add(Payment payment);

    Payment? Get(Guid id);
}
