using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Api.Mappings;

/// <summary>
/// Maps domain objects to API contracts at the presentation edge.
/// Mapping is explicit on purpose: a new field on <see cref="Payment"/> is only exposed
/// to merchants once it is deliberately mapped here.
/// </summary>
public static class PaymentMappings
{
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
