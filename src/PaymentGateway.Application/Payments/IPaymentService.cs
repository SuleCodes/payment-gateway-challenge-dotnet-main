using PaymentGateway.Common.Results;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Payments;

public interface IPaymentService
{
    Payment? GetPayment(Guid id);

    /// <summary>
    /// Sends the payment to the bank and stores the outcome (Authorized or Declined).
    /// Returns an error, and stores nothing, if the bank could not give a decision.
    /// </summary>
    Task<Result<Payment>> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken);
}
