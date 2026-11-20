using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Tests.Fakes;
using PaymentGateway.Application.Abstractions;
using PaymentGateway.Common.Results;

namespace PaymentGateway.Api.Tests;

public class PostPaymentTests
{
    private static readonly object ValidBody = new
    {
        cardNumber = "2222405343248877",
        expiryMonth = 4,
        expiryYear = 2030,
        currency = "GBP",
        amount = 100,
        cvv = "123"
    };

    [Fact]
    public async Task ValidRequest_Returns201_WithLocationOfRetrievablePayment()
    {
        var client = Client(new FakeBankClient(new BankAuthorization(true, "auth-1")));

        var response = await client.PostAsJsonAsync("/api/payments", ValidBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Authorized\"", json);
        Assert.Contains("\"cardNumberLastFour\":\"8877\"", json);
        Assert.DoesNotContain("2222405343248877", json);
        Assert.DoesNotContain("auth-1", json);

        var get = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_Returns400_AndDoesNotCallBank()
    {
        var bank = new FakeBankClient(new BankAuthorization(true, "auth-1"));

        var response = await Client(bank).PostAsJsonAsync("/api/payments", new { cardNumber = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(bank.Requests);
    }

    [Fact]
    public async Task BankUnavailable_Returns502()
    {
        var response = await Client(new FakeBankClient(FakeBankClient.Unavailable))
            .PostAsJsonAsync("/api/payments", ValidBody);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private static HttpClient Client(IBankClient bank) =>
        new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton(bank)))
            .CreateClient();
}
