using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Infrastructure.Bank;
using PaymentGateway.Infrastructure.Persistence;

using Polly;
using Polly.Retry;

namespace PaymentGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPaymentsRepository, PaymentsRepository>();

        services.AddOptions<BankOptions>()
            .Bind(configuration.GetSection(BankOptions.SectionName))
            .Validate(o => o.BaseUrl is { IsAbsoluteUri: true }, "Bank:BaseUrl must be an absolute URL.")
            .Validate(o => o.MaxRetryAttempts is >= 0 and <= 5, "Bank:MaxRetryAttempts must be between 0 and 5.")
            .ValidateOnStart();

        services.AddHttpClient<IBankClient, BankClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<BankOptions>>().Value;
                client.BaseAddress = options.BaseUrl;
                // HttpClient.Timeout wraps the whole handler pipeline, so it bounds all retries together.
                client.Timeout = options.Timeout;
            })
            .AddResilienceHandler("bank", (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<BankOptions>>().Value;
                if (options.MaxRetryAttempts == 0)
                {
                    return;
                }

                var logger = context.ServiceProvider.GetRequiredService<ILogger<BankClient>>();
                pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    Delay = options.RetryDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    // Retrying a payment is only safe when the bank cannot have processed it explicitly,
                    //  - 503: the bank explicitly refused to handle the request;
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>(ex => ex.HttpRequestError == HttpRequestError.ConnectionError)
                        .HandleResult(response => response.StatusCode == HttpStatusCode.ServiceUnavailable),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Bank call failed ({BankFailure}), retry {RetryAttempt} of {MaxRetryAttempts} in {RetryDelayMs} ms",
                            args.Outcome.Result is { } response ? (int)response.StatusCode : args.Outcome.Exception?.GetType().Name,
                            args.AttemptNumber + 1,
                            options.MaxRetryAttempts,
                            (int)args.RetryDelay.TotalMilliseconds);
                        return default;
                    },
                });
            });

        return services;
    }
}
