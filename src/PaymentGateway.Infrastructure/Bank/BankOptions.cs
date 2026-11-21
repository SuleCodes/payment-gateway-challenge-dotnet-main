namespace PaymentGateway.Infrastructure.Bank;

public sealed class BankOptions
{
    public const string SectionName = "Bank";

    /// <summary>Base URL of the acquiring bank, e.g. http://localhost:8080/.</summary>
    public Uri? BaseUrl { get; init; }

    /// <summary>
    /// Total time to wait for the bank, across all retry attempts, before treating it as unavailable.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Retries after the first attempt, only for failures where the bank cannot have processed the payment. 0 disables retries.</summary>
    public int MaxRetryAttempts { get; init; } = 2;

    /// <summary>Delay before the first retry. Later retries back off exponentially, with jitter.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(200);
}
