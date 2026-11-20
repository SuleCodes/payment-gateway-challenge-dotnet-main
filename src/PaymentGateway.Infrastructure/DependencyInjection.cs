using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Infrastructure.Bank;
using PaymentGateway.Infrastructure.Persistence;

namespace PaymentGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPaymentsRepository, PaymentsRepository>();

        services.AddOptions<BankOptions>()
            .Bind(configuration.GetSection(BankOptions.SectionName))
            .Validate(o => o.BaseUrl is { IsAbsoluteUri: true }, "Bank:BaseUrl must be an absolute URL.")
            .ValidateOnStart();

        services.AddHttpClient<IBankClient, BankClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BankOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
        });

        return services;
    }
}
