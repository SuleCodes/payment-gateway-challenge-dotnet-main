using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Tests.Fakes;
using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.Payments;
using PaymentGateway.Common.Results;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Infrastructure.Persistence;

namespace PaymentGateway.Api.Tests.Application;

public class PaymentServiceTests
{
    private static readonly ProcessPaymentCommand Command = new("2222405343248877", 4, 2027, "GBP", 100, "123");

    private readonly PaymentsRepository _repository = new();
    private readonly IdempotencyStore _idempotencyStore = new();

    [Theory]
    [InlineData(true, PaymentStatus.Authorized)]
    [InlineData(false, PaymentStatus.Declined)]
    public async Task StoresBankDecision_WithMaskedCardOnly(bool authorized, PaymentStatus expected)
    {
        var bank = new FakeBankClient(new BankAuthorization(authorized, authorized ? "auth-1" : null));

        var result = await Service(bank).ProcessPaymentAsync(Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Status);
        Assert.Equal("8877", result.Value.CardNumberLastFour);
        Assert.Same(result.Value, _repository.Get(result.Value.Id));
        Assert.Equal(Command.CardNumber, Assert.Single(bank.Requests).CardNumber); // full PAN goes to the bank only
    }

    [Fact]
    public async Task BankUnavailable_ReturnsError_AndStoresNothing()
    {
        var result = await Service(new FakeBankClient(FakeBankClient.Unavailable))
            .ProcessPaymentAsync(Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentErrors.BankUnavailable, result.Error);
    }

    [Fact]
    public async Task RepeatedIdempotencyKey_ReturnsOriginalPayment_WithoutCallingBankAgain()
    {
        var bank = new FakeBankClient(new BankAuthorization(true, "auth-1"));
        var command = Command with { IdempotencyKey = "key-1" };

        var first = await Service(bank).ProcessPaymentAsync(command, CancellationToken.None);
        var second = await Service(bank).ProcessPaymentAsync(command, CancellationToken.None);

        Assert.Same(first.Value, second.Value);
        Assert.Single(bank.Requests);
    }

    [Fact]
    public async Task IdempotencyKeyReusedForDifferentRequest_ReturnsError_WithoutCallingBank()
    {
        var bank = new FakeBankClient(new BankAuthorization(true, "auth-1"));
        await Service(bank).ProcessPaymentAsync(Command with { IdempotencyKey = "key-1" }, CancellationToken.None);

        var result = await Service(bank).ProcessPaymentAsync(
            Command with { IdempotencyKey = "key-1", Amount = 999 }, CancellationToken.None);

        Assert.Equal(PaymentErrors.IdempotencyKeyReused, result.Error);
        Assert.Single(bank.Requests);
    }

    [Fact]
    public async Task IdempotencyKeyInProgress_ReturnsError_WithoutCallingBank()
    {
        var bank = new FakeBankClient(new BankAuthorization(true, "auth-1"));
        _idempotencyStore.TryReserve("key-1", "another request's fingerprint"); // first request still waiting on the bank

        var result = await Service(bank).ProcessPaymentAsync(Command with { IdempotencyKey = "key-1" }, CancellationToken.None);

        Assert.Equal(PaymentErrors.IdempotencyKeyInProgress, result.Error);
        Assert.Empty(bank.Requests);
    }

    [Fact]
    public async Task BankUnavailable_FreesIdempotencyKey_SoTheMerchantCanRetry()
    {
        var command = Command with { IdempotencyKey = "key-1" };
        await Service(new FakeBankClient(FakeBankClient.Unavailable)).ProcessPaymentAsync(command, CancellationToken.None);

        var retry = await Service(new FakeBankClient(new BankAuthorization(true, "auth-1")))
            .ProcessPaymentAsync(command, CancellationToken.None);

        Assert.True(retry.IsSuccess);
    }

    private PaymentService Service(IBankClient bank) =>
        new(_repository, _idempotencyStore, bank, NullLogger<PaymentService>.Instance);
}
