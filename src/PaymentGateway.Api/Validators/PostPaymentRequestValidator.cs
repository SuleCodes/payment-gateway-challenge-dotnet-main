using FluentValidation;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Domain.Constants;

namespace PaymentGateway.Api.Validators;

/// <summary>
/// Validates a merchant's payment request against the rules in the brief.
/// A request that fails validation is "Rejected" and must never be sent to the bank.
/// </summary>
public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    private const string DigitsOnly = "^[0-9]+$";

    public PostPaymentRequestValidator(TimeProvider timeProvider)
    {
        // Report the first failure per field rather than every failing check (e.g. empty AND wrong length).
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.CardNumber)
            .NotEmpty()
            .Length(14, 19)
            .Matches(DigitsOnly).WithMessage("'{PropertyName}' must only contain numeric characters.");

        RuleFor(r => r.ExpiryMonth)
            .NotNull()
            .InclusiveBetween(1, 12);

        RuleFor(r => r.ExpiryYear)
            .NotNull();

        // Only checked once month and year are individually valid, to avoid piling up errors.
        RuleFor(r => r)
            .Must(r => IsInFuture(r.ExpiryMonth!.Value, r.ExpiryYear!.Value, timeProvider))
            .When(r => r.ExpiryMonth is >= 1 and <= 12 && r.ExpiryYear is not null)
            .WithName("ExpiryDate")
            .WithMessage("The card expiry date must be in the future.");

        RuleFor(r => r.Currency)
            .NotEmpty()
            .Length(3)
            .Must(c => c is not null && SupportedCurrencies.All.Contains(c))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", SupportedCurrencies.All)}.");

        RuleFor(r => r.Amount)
            .NotNull()
            .GreaterThan(0);

        RuleFor(r => r.Cvv)
            .NotEmpty()
            .Length(3, 4)
            .Matches(DigitsOnly).WithMessage("'{PropertyName}' must only contain numeric characters.");
    }

    /// <summary>
    /// A card is valid until the end of its expiry month, so the current month still counts as "in the future".
    /// </summary>
    private static bool IsInFuture(int month, int year, TimeProvider timeProvider)
    {
        if (year is < 1 or > 9999)
        {
            return false;
        }

        var today = timeProvider.GetUtcNow();

        return (year, month).CompareTo((today.Year, today.Month)) >= 0;
    }
}
