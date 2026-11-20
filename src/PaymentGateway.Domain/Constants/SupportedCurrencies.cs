namespace PaymentGateway.Domain.Constants;

/// <summary>
/// ISO 4217 currency codes the gateway accepts. The brief limits this to at most three.
/// </summary>
public static class SupportedCurrencies
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "GBP",
        "USD",
        "EUR"
    };
}
