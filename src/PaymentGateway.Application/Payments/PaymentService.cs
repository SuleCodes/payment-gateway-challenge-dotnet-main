using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentsRepository paymentsRepository, ILogger<PaymentService> logger)
    {
        _paymentsRepository = paymentsRepository;
        _logger = logger;
    }

    public Payment? GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null)
        {
            _logger.LogInformation("Payment {PaymentId} not found", id);
            return null;
        }

        _logger.LogInformation("Payment {PaymentId} retrieved with status {PaymentStatus}", id, payment.Status);
        return payment;
    }
}
