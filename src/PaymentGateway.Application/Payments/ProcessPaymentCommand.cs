namespace PaymentGateway.Application.Payments;

/// <summary>
/// A validated request to process a payment. Contains the full card number and CVV, which are
/// passed to the bank and never stored.
/// </summary>
public sealed record ProcessPaymentCommand(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv);
