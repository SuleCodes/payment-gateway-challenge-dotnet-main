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

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    // Mirrors the API's serializer settings: enums are sent as strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Random _random = new();
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services
                .AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task SerialisesStatusAndLastFourAsStrings()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            ExpiryYear = 2030,
            ExpiryMonth = 4,
            Amount = 100,
            CardNumberLastFour = "0123",
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var client = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services => services
                .AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();

        // Act
        var json = await client.GetStringAsync($"/api/Payments/{payment.Id}");

        // Assert: raw JSON, so the contract itself is checked rather than what a lenient deserialiser accepts
        Assert.Contains("\"status\":\"Authorized\"", json);
        Assert.Contains("\"cardNumberLastFour\":\"0123\"", json);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
