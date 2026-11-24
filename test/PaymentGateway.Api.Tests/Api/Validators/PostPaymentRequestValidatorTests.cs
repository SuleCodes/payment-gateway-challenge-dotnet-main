using FluentValidation.TestHelper;

using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Tests.Api.Validators;

public class PostPaymentRequestValidatorTests
{
    // Fixed "now" so expiry rules are deterministic: 20 November 2026.
    private readonly PostPaymentRequestValidator _validator =
        new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)));

    private static readonly PostPaymentRequest Valid = new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = 2027,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    // Boundary values that must be accepted.
    public static TheoryData<PostPaymentRequest> ValidRequests => new()
    {
        Valid,
        Valid with { CardNumber = "12345678901234" },      // 14 digits (min)
        Valid with { CardNumber = "1234567890123456789" }, // 19 digits (max)
        Valid with { ExpiryMonth = 11, ExpiryYear = 2026 }, // current month: valid until month end
        Valid with { Cvv = "0123" },                       // 4 digits, leading zero kept
    };

    // One case per rule, on or just past its boundary.
    public static TheoryData<PostPaymentRequest, string> InvalidRequests => new()
    {
        { Valid with { CardNumber = null }, "CardNumber" },
        { Valid with { CardNumber = "1234567890123" }, "CardNumber" },        // 13 digits
        { Valid with { CardNumber = "12345678901234567890" }, "CardNumber" }, // 20 digits
        { Valid with { CardNumber = "1234-5678-9012-34" }, "CardNumber" },    // non-numeric
        { Valid with { ExpiryMonth = 13 }, "ExpiryMonth" },
        { Valid with { ExpiryYear = null }, "ExpiryYear" },
        { Valid with { ExpiryMonth = 10, ExpiryYear = 2026 }, "ExpiryDate" }, // last month
        { Valid with { Currency = "JPY" }, "Currency" },                      // real code, not supported
        { Valid with { Amount = 0 }, "Amount" },
        { Valid with { Cvv = "12" }, "Cvv" },
        { Valid with { Cvv = "12a" }, "Cvv" },
    };

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void Accepts(PostPaymentRequest request)
    {
        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Rejects(PostPaymentRequest request, string property)
    {
        _validator.TestValidate(request).ShouldHaveValidationErrorFor(property);
    }
}
