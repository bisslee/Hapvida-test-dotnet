using FluentAssertions;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Services;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using InfraBrasilApiClient = Hapvida.ExternalIntegration.Infra.ExternalApis.BrasilApiClient;
using InfraViaCepClient = Hapvida.ExternalIntegration.Infra.ExternalApis.ViaCepClient;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Services;

// Classe helper para criar providers mockáveis mantendo o nome do tipo
internal class BrasilApiClient : ICepProvider
{
    private readonly Func<ZipCode, CancellationToken, Task<CepResult?>> _getCepFunc;

    public BrasilApiClient(HttpClient httpClient, Func<ZipCode, CancellationToken, Task<CepResult?>> getCepFunc)
    {
        _getCepFunc = getCepFunc;
    }

    public Task<CepResult?> GetCepAsync(ZipCode zipCode, CancellationToken cancellationToken = default)
    {
        return _getCepFunc(zipCode, cancellationToken);
    }
}

internal class ViaCepClient : ICepProvider
{
    private readonly Func<ZipCode, CancellationToken, Task<CepResult?>> _getCepFunc;

    public ViaCepClient(HttpClient httpClient, Func<ZipCode, CancellationToken, Task<CepResult?>> getCepFunc)
    {
        _getCepFunc = getCepFunc;
    }

    public Task<CepResult?> GetCepAsync(ZipCode zipCode, CancellationToken cancellationToken = default)
    {
        return _getCepFunc(zipCode, cancellationToken);
    }
}

public class CepServiceTests : BaseTest
{
    private readonly Mock<ILogger<CepService>> LoggerMock;
    private readonly HttpClient HttpClient;

    public CepServiceTests()
    {
        LoggerMock = new Mock<ILogger<CepService>>();
        HttpClient = new HttpClient();
    }

    private CepService CreateService(
        Func<ZipCode, CancellationToken, Task<CepResult?>>? primaryFunc = null,
        Func<ZipCode, CancellationToken, Task<CepResult?>>? fallbackFunc = null)
    {
        var primaryProvider = primaryFunc != null
            ? new BrasilApiClient(HttpClient, primaryFunc)
            : (ICepProvider)new InfraBrasilApiClient(HttpClient);

        var fallbackProvider = fallbackFunc != null
            ? new ViaCepClient(HttpClient, fallbackFunc)
            : (ICepProvider)new InfraViaCepClient(HttpClient);

        var providers = new List<ICepProvider>
        {
            primaryProvider,
            fallbackProvider
        };

        return new CepService(providers, LoggerMock.Object);
    }

    [Fact]
    public async Task GetCepAsync_Should_Return_Result_From_Primary_Provider()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var expectedResult = new CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "brasilapi"
        };

        var service = CreateService(
            primaryFunc: (z, ct) => Task.FromResult<CepResult?>(expectedResult)
        );

        // Act
        var result = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("brasilapi");
        result.City.Should().Be("São Paulo");
    }

    [Fact]
    public async Task GetCepAsync_Should_Use_Fallback_When_Primary_Returns_Null()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var fallbackResult = new CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "viacep"
        };

        var service = CreateService(
            primaryFunc: (z, ct) => Task.FromResult<CepResult?>(null),
            fallbackFunc: (z, ct) => Task.FromResult<CepResult?>(fallbackResult)
        );

        // Act
        var result = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("viacep");
    }

    [Fact]
    public async Task GetCepAsync_Should_Use_Fallback_When_Primary_Throws_Exception()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var fallbackResult = new CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "viacep"
        };

        var service = CreateService(
            primaryFunc: (z, ct) => throw new HttpRequestException("Erro na BrasilAPI"),
            fallbackFunc: (z, ct) => Task.FromResult<CepResult?>(fallbackResult)
        );

        // Act
        var result = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("viacep");
    }

    [Fact]
    public async Task GetCepAsync_Should_Return_Null_When_Both_Providers_Return_Null()
    {
        // Arrange
        var zipCode = ZipCode.Create("99999999");

        var service = CreateService(
            primaryFunc: (z, ct) => Task.FromResult<CepResult?>(null),
            fallbackFunc: (z, ct) => Task.FromResult<CepResult?>(null)
        );

        // Act
        var result = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCepAsync_Should_Throw_Exception_When_Fallback_Throws()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");

        var service = CreateService(
            primaryFunc: (z, ct) => Task.FromResult<CepResult?>(null),
            fallbackFunc: (z, ct) => throw new HttpRequestException("Erro na ViaCEP")
        );

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetCepAsync(zipCode, CancellationToken.None));
    }
}
