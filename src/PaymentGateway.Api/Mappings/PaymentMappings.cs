using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Api.Mappings;

/// <summary>
/// Maps between API contracts and the application/domain at the presentation edge.
/// Mapping is explicit on purpose: a new field on <see cref="Payment"/> is only exposed
/// to merchants once it is deliberately mapped here.
/// </summary>
public static class PaymentMappings
{
    /// <summary>Only call on a request that has passed validation: required fields are assumed present.</summary>
    public static ProcessPaymentCommand ToCommand(this PostPaymentRequest request, string? idempotencyKey) => new(
        request.CardNumber!,
        request.ExpiryMonth!.Value,
        request.ExpiryYear!.Value,
        request.Currency!,
        request.Amount!.Value,
        request.Cvv!,
        idempotencyKey);

    public static PostPaymentResponse ToPostPaymentResponse(this Payment payment) => new()
    {
        Id = payment.Id,
        Status = payment.Status,
        CardNumberLastFour = payment.CardNumberLastFour,
        ExpiryMonth = payment.ExpiryMonth,
        ExpiryYear = payment.ExpiryYear,
        Currency = payment.Currency,
        Amount = payment.Amount
    };

    public static GetPaymentResponse ToGetPaymentResponse(this Payment payment) => new()
    {
        Id = payment.Id,
        Status = payment.Status,
        CardNumberLastFour = payment.CardNumberLastFour,
        ExpiryMonth = payment.ExpiryMonth,
        ExpiryYear = payment.ExpiryYear,
        Currency = payment.Currency,
        Amount = payment.Amount
    };
}
