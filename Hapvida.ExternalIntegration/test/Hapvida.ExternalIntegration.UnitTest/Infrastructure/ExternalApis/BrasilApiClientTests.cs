using FluentAssertions;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.Infra.ExternalApis;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Infrastructure.ExternalApis;

public class BrasilApiClientTests : BaseTest
{
    private readonly Mock<ILogger<BrasilApiClient>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly BrasilApiClient _client;

    public BrasilApiClientTests()
    {
        _loggerMock = new Mock<ILogger<BrasilApiClient>>();
        _httpClient = new HttpClient();
        _client = new BrasilApiClient(_httpClient);
    }

    [Fact]
    public async Task GetCepAsync_Should_Return_CepResult_When_Successful()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var expectedResponse = new
        {
            street = "Avenida Paulista",
            neighborhood = "Bela Vista",
            city = "São Paulo",
            state = "SP",
            ibge = "3550308",
            coordinates = new { latitude = -23.5505, longitude = -46.6333 }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://brasilapi.com.br") };
        var client = new BrasilApiClient(httpClient);

        // Act
        var result = await client.GetCepAsync(zipCode);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("São Paulo");
        result.State.Should().Be("SP");
        result.Street.Should().Be("Avenida Paulista");
        result.District.Should().Be("Bela Vista");
        result.Provider.Should().Be("brasilapi");
        result.Location.Should().NotBeNull();
        result.Location!.Latitude.Should().Be(-23.5505);
        result.Location.Longitude.Should().Be(-46.6333);
    }

    [Fact]
    public async Task GetCepAsync_Should_Return_Null_When_NotFound()
    {
        // Arrange
        var zipCode = ZipCode.Create("99999999");
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://brasilapi.com.br") };
        var client = new BrasilApiClient(httpClient);

        // Act
        var result = await client.GetCepAsync(zipCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCepAsync_Should_Return_Null_When_Response_Is_Null()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "null");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://brasilapi.com.br") };
        var client = new BrasilApiClient(httpClient);

        // Act
        var result = await client.GetCepAsync(zipCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCepAsync_Should_Throw_HttpRequestException_On_Error()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "Internal Server Error");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://brasilapi.com.br") };
        var client = new BrasilApiClient(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetCepAsync(zipCode));
    }

    [Fact]
    public async Task GetCepAsync_Should_Handle_Response_Without_Coordinates()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var expectedResponse = new
        {
            street = "Avenida Paulista",
            neighborhood = "Bela Vista",
            city = "São Paulo",
            state = "SP",
            ibge = "3550308"
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://brasilapi.com.br") };
        var client = new BrasilApiClient(httpClient);

        // Act
        var result = await client.GetCepAsync(zipCode);

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().BeNull();
    }

    // Helper class para mockar HttpResponseMessage
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}

