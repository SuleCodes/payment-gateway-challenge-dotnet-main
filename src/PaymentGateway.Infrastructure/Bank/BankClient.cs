using System.Diagnostics;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Common.Results;

namespace PaymentGateway.Infrastructure.Bank;

/// <summary>
/// Typed HttpClient for the acquiring bank. Translates between the application's
/// <see cref="BankAuthorizationRequest"/> and the bank's HTTP contract, and turns transport
/// failures into an <see cref="Error"/> instead of letting exceptions escape.
/// </summary>
public class BankClient : IBankClient
{
    private static readonly Error Unavailable = new("Bank.Unavailable", "The bank did not return a decision.");

    private readonly HttpClient _httpClient;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<BankAuthorization>> AuthorizeAsync(BankAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var body = new BankPaymentRequest(
            request.CardNumber,
            $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            request.Currency,
            request.Amount,
            request.Cvv);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("payments", body, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // 5xx: bank-side problem. 4xx: we sent something the bank rejected, which is a bug on our side.
                _logger.Log(
                    (int)response.StatusCode >= 500 ? LogLevel.Warning : LogLevel.Error,
                    "Bank returned {BankStatusCode} after {ElapsedMs} ms",
                    (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                return Unavailable;
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken);
            if (bankResponse is null)
            {
                _logger.LogError("Bank returned an empty response after {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
                return Unavailable;
            }

            _logger.LogInformation(
                "Bank responded Authorized={BankAuthorized} after {ElapsedMs} ms",
                bankResponse.Authorized, stopwatch.ElapsedMilliseconds);

            return new BankAuthorization(
                bankResponse.Authorized,
                string.IsNullOrEmpty(bankResponse.AuthorizationCode) ? null : bankResponse.AuthorizationCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Network failure, unreadable body, or HttpClient timeout. A cancellation requested by the
            // caller (merchant disconnected) is not caught and propagates as normal.
            _logger.LogWarning(ex, "Bank call failed after {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
            return Unavailable;
        }
    }
}
