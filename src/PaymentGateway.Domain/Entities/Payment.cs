using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

/// <summary>
/// A payment as the gateway records it. Independent of any API contract or storage technology.
/// Only masked card data is held: never the full card number or CVV.
/// </summary>
public class Payment
{
    public required Guid Id { get; init; }
    public required PaymentStatus Status { get; init; }
    public required string CardNumberLastFour { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public required int Amount { get; init; }
}
