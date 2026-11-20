using PaymentGateway.Application.Abstractions;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentService(IPaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
    }

    public Payment? GetPayment(Guid id) => _paymentsRepository.Get(id);
}
