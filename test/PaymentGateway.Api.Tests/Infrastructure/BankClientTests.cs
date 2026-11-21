using System.Net;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Infrastructure;
using PaymentGateway.Infrastructure.Bank;

namespace PaymentGateway.Api.Tests.Infrastructure;

public class BankClientTests
{
    private static readonly BankAuthorizationRequest Request = new("2222405343248877", 4, 2027, "GBP", 100, "123");

    private static readonly HttpRequestException ConnectionRefused = new(HttpRequestError.ConnectionError);

    [Fact]
    public async Task SendsBankContract_AndMapsAuthorizedResponse()
    {
        var handler = new StubHandler(HttpStatusCode.OK);

        var result = await Client(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.Equal(
            """{"card_number":"2222405343248877","expiry_date":"04/2027","currency":"GBP","amount":100,"cvv":"123"}""",
            handler.RequestBody);
        Assert.True(result.IsSuccess);
        Assert.Equal(new BankAuthorization(true, "abc"), result.Value);
    }

    [Fact]
    public async Task ServiceUnavailable_ReturnsError()
    {
        var result = await Client(new StubHandler(HttpStatusCode.ServiceUnavailable))
            .AuthorizeAsync(Request, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    public static TheoryData<object[], bool, int> RetryCases => new()
    {
        // Bank could not have processed the payment: retried.
        { [HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK], true, 2 },
        { [ConnectionRefused, HttpStatusCode.OK], true, 2 },
        // Gives up after MaxRetryAttempts (2) retries.
        { [HttpStatusCode.ServiceUnavailable], false, 3 },
        // Bank may have processed the payment: never retried, to avoid charging twice.
        { [HttpStatusCode.InternalServerError, HttpStatusCode.OK], false, 1 },
        { [HttpStatusCode.GatewayTimeout, HttpStatusCode.OK], false, 1 },
    };

    [Theory]
    [MemberData(nameof(RetryCases))]
    public async Task RetriesOnlyWhenBankCannotHaveProcessedThePayment(object[] outcomes, bool expectSuccess, int expectedCalls)
    {
        var handler = new StubHandler(outcomes);

        var result = await ClientWithRetries(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.Equal(expectSuccess, result.IsSuccess);
        Assert.Equal(expectedCalls, handler.Calls);
    }

    private static BankClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://bank/") }, NullLogger<BankClient>.Instance);

    /// <summary>The real DI registration (including the Polly pipeline), with the network swapped for a stub.</summary>
    private static IBankClient ClientWithRetries(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bank:BaseUrl"] = "http://bank/",
                ["Bank:MaxRetryAttempts"] = "2",
                ["Bank:RetryDelay"] = "00:00:00",
            })
            .Build();

        var services = new ServiceCollection().AddLogging().AddInfrastructure(configuration);
        services.AddHttpClient<IBankClient, BankClient>().ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider().GetRequiredService<IBankClient>();
    }

    /// <summary>Returns each outcome in turn (a status code, or an exception to throw), repeating the last one.</summary>
    private sealed class StubHandler(params object[] outcomes) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var outcome = outcomes[Math.Min(Calls++, outcomes.Length - 1)];

            if (outcome is Exception exception)
            {
                throw exception;
            }

            return new HttpResponseMessage((HttpStatusCode)outcome)
            {
                Content = new StringContent("""{"authorized":true,"authorization_code":"abc"}""", Encoding.UTF8, "application/json"),
            };
        }
    }
}
