namespace PaymentGateway.Application.Abstractions;

/// <summary>
/// Remembers which payment each <c>Idempotency-Key</c> produced, so a request the merchant
/// sends again is answered from the original result instead of going to the bank twice.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically reserves <paramref name="key"/> for a new request. Returns <c>null</c> if this call
    /// made the reservation, otherwise the entry that already exists for the key.
    /// </summary>
    IdempotencyEntry? TryReserve(string key, string requestFingerprint);

    /// <summary>Records the payment created for a reserved key.</summary>
    void Complete(string key, Guid paymentId);

    /// <summary>Frees a reserved key when no payment was created, so the merchant can retry with it.</summary>
    void Release(string key);
}

/// <param name="RequestFingerprint">Keyed hash of the request the key was first used with.</param>
/// <param name="PaymentId"><c>null</c> while the original request is still being processed.</param>
public sealed record IdempotencyEntry(string RequestFingerprint, Guid? PaymentId);
