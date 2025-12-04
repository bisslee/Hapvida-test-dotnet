using FluentAssertions;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.Infra.ExternalApis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Infrastructure.ExternalApis;

public class ViaCepClientTests : BaseTest
{
    [Fact]
    public async Task GetCepAsync_Should_Return_CepResult_When_Successful()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var expectedResponse = new
        {
            cep = "01306-001",
            logradouro = "Avenida Paulista",
            bairro = "Bela Vista",
            localidade = "São Paulo",
            uf = "SP",
            ibge = "3550308",
            erro = false
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br") };
        var client = new ViaCepClient(httpClient);

        // Act
        var result = await client.GetCepAsync(zipCode);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("São Paulo");
        result.State.Should().Be("SP");
        result.Street.Should().Be("Avenida Paulista");
        result.District.Should().Be("Bela Vista");
        result.Provider.Should().Be("viacep");
        result.Location.Should().BeNull(); // ViaCEP não retorna coordenadas
    }

    [Fact]
    public async Task GetCepAsync_Should_Return_Null_When_Erro_Is_True()
    {
        // Arrange
        var zipCode = ZipCode.Create("99999999");
        var expectedResponse = new
        {
            erro = true
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br") };
        var client = new ViaCepClient(httpClient);

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
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br") };
        var client = new ViaCepClient(httpClient);

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
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br") };
        var client = new ViaCepClient(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetCepAsync(zipCode));
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

