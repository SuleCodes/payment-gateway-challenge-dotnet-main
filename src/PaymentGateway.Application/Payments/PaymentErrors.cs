using PaymentGateway.Common.Results;

namespace PaymentGateway.Application.Payments;

public static class PaymentErrors
{
    public static readonly Error BankUnavailable = new(
        "Payment.BankUnavailable",
        "The acquiring bank could not process the payment. No payment was created; it is safe to retry.");

    public static readonly Error IdempotencyKeyReused = new(
        "Payment.IdempotencyKeyReused",
        "This Idempotency-Key was already used for a different request. Use a new key for a new payment.");

    public static readonly Error IdempotencyKeyInProgress = new(
        "Payment.IdempotencyKeyInProgress",
        "A request with this Idempotency-Key is still being processed. Retry shortly.");
}
