using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Abstractions;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Infrastructure.Persistence;

namespace PaymentGateway.Api.Tests.Api.Controllers;

public class GetPaymentTests
{
    // Mirrors the API's serializer settings: enums are sent as strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Fixed data, chosen to cover edge cases: month 12 and a last four with a leading zero.
    private static readonly Payment StoredPayment = new()
    {
        Id = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
        Status = PaymentStatus.Authorized,
        CardNumberLastFour = "0123",
        ExpiryMonth = 12,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 1050,
        AuthorizationCode = "auth-secret"
    };

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Act
        var response = await ClientWithStoredPayment().GetAsync($"/api/v1/payments/{StoredPayment.Id}");
        var body = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(StoredPayment.Id, body.Id);
        Assert.Equal(PaymentStatus.Authorized, body.Status);
        Assert.Equal("0123", body.CardNumberLastFour);
        Assert.Equal(12, body.ExpiryMonth);
        Assert.Equal(2030, body.ExpiryYear);
        Assert.Equal("GBP", body.Currency);
        Assert.Equal(1050, body.Amount);
    }

    [Fact]
    public async Task SerialisesContract_AndDoesNotExposeAuthorizationCode()
    {
        // Act
        var json = await ClientWithStoredPayment().GetStringAsync($"/api/v1/payments/{StoredPayment.Id}");

        // Assert: raw JSON, so the contract itself is checked rather than what a lenient deserialiser accepts
        Assert.Contains("\"status\":\"Authorized\"", json);
        Assert.Contains("\"cardNumberLastFour\":\"0123\"", json);
        Assert.DoesNotContain("auth-secret", json);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Act
        var response = await ClientWithStoredPayment().GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpClient ClientWithStoredPayment()
    {
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(StoredPayment);

        return new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services => services
                .AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();
    }
}
