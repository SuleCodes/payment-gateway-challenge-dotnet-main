using PaymentGateway.Common.Results;

namespace PaymentGateway.Application.Payments;

public static class PaymentErrors
{
    public static readonly Error BankUnavailable = new(
        "Payment.BankUnavailable",
        "The acquiring bank could not process the payment. No payment was created; it is safe to retry.");
}
