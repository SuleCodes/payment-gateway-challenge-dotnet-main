using System.Net;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Infrastructure.Bank;

namespace PaymentGateway.Api.Tests.Infrastructure;

public class BankClientTests
{
    private static readonly BankAuthorizationRequest Request = new("2222405343248877", 4, 2027, "GBP", 100, "123");

    [Fact]
    public async Task SendsBankContract_AndMapsAuthorizedResponse()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"authorized":true,"authorization_code":"abc"}""");

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
        var result = await Client(new StubHandler(HttpStatusCode.ServiceUnavailable, "{}"))
            .AuthorizeAsync(Request, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    private static BankClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://bank/") }, NullLogger<BankClient>.Instance);

    private sealed class StubHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") };
        }
    }
}
