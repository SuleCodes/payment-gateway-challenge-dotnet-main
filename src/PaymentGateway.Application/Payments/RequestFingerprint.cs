using System.Security.Cryptography;
using System.Text;

namespace PaymentGateway.Application.Payments;

/// <summary>
/// Identifies a payment request, so a reused <c>Idempotency-Key</c> can be checked against the request it was first used with.
/// </summary>
internal static class RequestFingerprint
{
    // Keyed hash (HMAC), generated on startup but should be replaced with secret in KVault
    //TODO: Replace Key with secret from Keyvault 
    private static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);

    public static string For(ProcessPaymentCommand command)
    {
        var data = string.Join('|',
            command.CardNumber, command.ExpiryMonth, command.ExpiryYear, command.Currency, command.Amount, command.Cvv);
        return Convert.ToHexString(HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes(data)));
    }
}
