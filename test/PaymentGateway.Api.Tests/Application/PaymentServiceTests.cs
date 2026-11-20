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

    private PaymentService Service(IBankClient bank) => new(_repository, bank, NullLogger<PaymentService>.Instance);
}
