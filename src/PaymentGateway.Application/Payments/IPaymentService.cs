using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Payments;

public interface IPaymentService
{
    Payment? GetPayment(Guid id);
}
