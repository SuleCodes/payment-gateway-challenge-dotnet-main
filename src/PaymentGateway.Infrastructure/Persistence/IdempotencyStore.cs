using System.Collections.Concurrent;

using PaymentGateway.Application.Abstractions;

namespace PaymentGateway.Infrastructure.Persistence;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new();

    public IdempotencyEntry? TryReserve(string key, string requestFingerprint)
    {
        var reservation = new IdempotencyEntry(requestFingerprint, PaymentId: null);
        var entry = _entries.GetOrAdd(key, reservation);
        return ReferenceEquals(entry, reservation) ? null : entry;
    }

    public void Complete(string key, Guid paymentId) =>
        _entries[key] = _entries[key] with { PaymentId = paymentId };

    public void Release(string key) => _entries.TryRemove(key, out _);
}
