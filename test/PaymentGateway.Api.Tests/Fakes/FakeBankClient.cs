using PaymentGateway.Application.Abstractions;
using PaymentGateway.Common.Results;

namespace PaymentGateway.Api.Tests.Fakes;

/// <summary>Returns a preset bank result and records what it was asked.</summary>
public class FakeBankClient : IBankClient
{
    public static readonly Error Unavailable = new("Bank.Unavailable", "test");

    private readonly Result<BankAuthorization> _result;

    public FakeBankClient(Result<BankAuthorization> result) => _result = result;

    public List<BankAuthorizationRequest> Requests { get; } = new();

    public Task<Result<BankAuthorization>> AuthorizeAsync(BankAuthorizationRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_result);
    }
}
