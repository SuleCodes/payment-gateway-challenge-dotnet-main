using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Common.Results;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IBankClient _bankClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentsRepository paymentsRepository,
        IIdempotencyStore idempotencyStore,
        IBankClient bankClient,
        ILogger<PaymentService> logger)
    {
        _paymentsRepository = paymentsRepository;
        _idempotencyStore = idempotencyStore;
        _bankClient = bankClient;
        _logger = logger;
    }

    public Payment? GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null)
        {
            _logger.LogInformation("Payment {PaymentId} not found", id);
            return null;
        }

        _logger.LogInformation("Payment {PaymentId} retrieved with status {PaymentStatus}", id, payment.Status);
        return payment;
    }

    public async Task<Result<Payment>> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        if (command.IdempotencyKey is not { } key)
        {
            return await AuthorizeAndStoreAsync(command, cancellationToken);
        }

        var fingerprint = RequestFingerprint.For(command);
        var existing = _idempotencyStore.TryReserve(key, fingerprint);
        if (existing is not null)
        {
            return FromExistingEntry(key, fingerprint, existing);
        }

        var completed = false;
        try
        {
            var result = await AuthorizeAndStoreAsync(command, cancellationToken);
            if (result.IsSuccess)
            {
                _idempotencyStore.Complete(key, result.Value.Id);
                completed = true;
            }

            return result;
        }
        finally
        {
            // No payment was created (bank unavailable, cancelled, or an unexpected error),
            // so free the key: the merchant can retry with it.
            if (!completed)
            {
                _idempotencyStore.Release(key);
            }
        }
    }

    private Result<Payment> FromExistingEntry(string key, string fingerprint, IdempotencyEntry existing)
    {
        if (existing.PaymentId is not { } paymentId)
        {
            _logger.LogInformation("Payment request rejected: idempotency key {IdempotencyKey} is still in progress", key);
            return PaymentErrors.IdempotencyKeyInProgress;
        }

        if (existing.RequestFingerprint != fingerprint)
        {
            _logger.LogWarning("Payment request rejected: idempotency key {IdempotencyKey} reused for a different request", key);
            return PaymentErrors.IdempotencyKeyReused;
        }

        var payment = _paymentsRepository.Get(paymentId)
            ?? throw new InvalidOperationException($"Idempotency key {key} points to missing payment {paymentId}.");

        _logger.LogInformation(
            "Payment {PaymentId} returned for repeated idempotency key {IdempotencyKey}; bank not called",
            payment.Id, key);
        return payment;
    }

    private async Task<Result<Payment>> AuthorizeAndStoreAsync(ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        var bankResult = await _bankClient.AuthorizeAsync(
            new BankAuthorizationRequest(
                command.CardNumber,
                command.ExpiryMonth,
                command.ExpiryYear,
                command.Currency,
                command.Amount,
                command.Cvv),
            cancellationToken);

        if (!bankResult.IsSuccess)
        {
            _logger.LogWarning(
                "Payment not processed: bank returned no decision ({ErrorCode}) for {Amount} {Currency}",
                bankResult.Error.Code, command.Amount, command.Currency);
            return PaymentErrors.BankUnavailable;
        }

        // Only masked card data is kept: the full card number and CVV are never stored.
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = bankResult.Value.IsAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            CardNumberLastFour = command.CardNumber[^4..],
            ExpiryMonth = command.ExpiryMonth,
            ExpiryYear = command.ExpiryYear,
            Currency = command.Currency,
            Amount = command.Amount,
            AuthorizationCode = bankResult.Value.AuthorizationCode
        };

        _paymentsRepository.Add(payment);

        _logger.LogInformation(
            "Payment {PaymentId} processed with status {PaymentStatus} for {Amount} {Currency}",
            payment.Id, payment.Status, payment.Amount, payment.Currency);

        return payment;
    }
}
