using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.Bank;

// Wire contract of the bank simulator. Kept internal to Infrastructure.

internal sealed record BankPaymentRequest(
    [property: JsonPropertyName("card_number")] string CardNumber,
    [property: JsonPropertyName("expiry_date")] string ExpiryDate,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("cvv")] string Cvv);

internal sealed record BankPaymentResponse(
    [property: JsonPropertyName("authorized")] bool Authorized,
    [property: JsonPropertyName("authorization_code")] string? AuthorizationCode);
