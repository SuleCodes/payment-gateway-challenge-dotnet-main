using PaymentGateway.Common.Results;

namespace PaymentGateway.Application.Abstractions;

/// <summary>
/// Port to the acquiring bank. Defined in the application's own terms; the HTTP contract of the
/// bank (snake_case JSON, MM/YYYY expiry) is an Infrastructure detail.
/// </summary>
public interface IBankClient
{
    /// <returns>The bank's decision, or an error if the bank could not give one (unavailable, timeout, bad response).</returns>
    Task<Result<BankAuthorization>> AuthorizeAsync(BankAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record BankAuthorizationRequest(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv);

public sealed record BankAuthorization(bool IsAuthorized, string? AuthorizationCode);
