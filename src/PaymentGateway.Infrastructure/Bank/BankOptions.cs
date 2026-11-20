namespace PaymentGateway.Infrastructure.Bank;

public sealed class BankOptions
{
    public const string SectionName = "Bank";

    /// <summary>Base URL of the acquiring bank, e.g. http://localhost:8080/.</summary>
    public Uri? BaseUrl { get; init; }

    /// <summary>How long to wait for the bank before treating it as unavailable.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
