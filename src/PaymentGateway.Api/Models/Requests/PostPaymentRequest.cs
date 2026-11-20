namespace PaymentGateway.Api.Models.Requests;

/// <summary>
/// Merchant request to process a card payment.
/// Properties are nullable on purpose: this models what the merchant actually sent, so a missing
/// field reaches the validator (and gets a clear error) instead of silently defaulting to 0 or failing
/// JSON deserialisation with a less helpful message.
/// </summary>
public record PostPaymentRequest
{
    /// <summary>Full card number (PAN), 14-19 digits. A string so leading zeros are preserved.</summary>
    public string? CardNumber { get; init; }

    /// <summary>Expiry month, 1-12.</summary>
    public int? ExpiryMonth { get; init; }

    /// <summary>Four-digit expiry year.</summary>
    public int? ExpiryYear { get; init; }

    /// <summary>ISO 4217 currency code, e.g. GBP.</summary>
    public string? Currency { get; init; }

    /// <summary>Amount in the minor currency unit, e.g. 1050 = £10.50.</summary>
    public int? Amount { get; init; }

    /// <summary>Card security code, 3-4 digits. A string so leading zeros are preserved.</summary>
    public string? Cvv { get; init; }
}
