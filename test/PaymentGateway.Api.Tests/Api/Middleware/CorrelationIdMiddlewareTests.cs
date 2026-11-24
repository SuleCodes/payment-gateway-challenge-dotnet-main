using Microsoft.AspNetCore.Mvc.Testing;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Tests.Api.Middleware;

public class CorrelationIdMiddlewareTests : IClassFixture<WebApplicationFactory<PaymentsController>>
{
    private readonly HttpClient _client;

    public CorrelationIdMiddlewareTests(WebApplicationFactory<PaymentsController> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EchoesCorrelationIdFromRequest()
    {
        var response = await SendAsync("merchant-abc-123");

        Assert.Equal("merchant-abc-123", ResponseCorrelationId(response));
    }

    [Theory]
    [InlineData(null)]                     // missing
    [InlineData("bad value\r\ninjected")]  // unsafe characters
    public async Task GeneratesCorrelationId_WhenMissingOrInvalid(string? incoming)
    {
        var response = await SendAsync(incoming);

        Assert.True(Guid.TryParse(ResponseCorrelationId(response), out _));
    }

    private Task<HttpResponseMessage> SendAsync(string? correlationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/payments/{Guid.NewGuid()}");
        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return _client.SendAsync(request);
    }

    private static string ResponseCorrelationId(HttpResponseMessage response) =>
        Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName));
}
