namespace PaymentGateway.Application.Payments;

/// <summary>
/// A validated request to process a payment. Contains the full card number and CVV, which are
/// passed to the bank and never stored. <c>IdempotencyKey</c> is optional and not part of the payment itself.
/// </summary>
public sealed record ProcessPaymentCommand(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv,
    string? IdempotencyKey = null);
