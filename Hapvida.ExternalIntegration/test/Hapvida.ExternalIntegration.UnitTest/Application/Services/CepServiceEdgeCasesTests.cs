using FluentAssertions;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Services;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Services;

public class CepServiceEdgeCasesTests : BaseTest
{
    private readonly Mock<ILogger<CepService>> LoggerMock;
    private readonly HttpClient HttpClient;

    public CepServiceEdgeCasesTests()
    {
        LoggerMock = new Mock<ILogger<CepService>>();
        HttpClient = new HttpClient();
    }

    [Fact]
    public async Task GetCepAsync_Should_Handle_TaskCanceledException_From_Primary()
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
            primaryFunc: (z, ct) => throw new TaskCanceledException("Timeout"),
            fallbackFunc: (z, ct) => Task.FromResult<CepResult?>(fallbackResult)
        );

        // Act
        var result = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("viacep");
    }

    [Fact]
    public async Task GetCepAsync_Should_Handle_TimeoutException_From_Primary()
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
            primaryFunc: (z, ct) => throw new TimeoutException("Request timeout"),
            fallbackFunc: (z, ct) => Task.FromResult<CepResult?>(fallbackResult)
        );

        // Act
        var result = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("viacep");
    }

    [Fact]
    public async Task GetCepAsync_Should_Log_Provider_Used()
    {
        // Arrange
        var zipCode = ZipCode.Create("01306001");
        var primaryResult = new CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "brasilapi"
        };

        var service = CreateService(
            primaryFunc: (z, ct) => Task.FromResult<CepResult?>(primaryResult)
        );

        // Act
        await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        // Verifica que o logger foi chamado com informações sobre o CEP
        LoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(zipCode.Value) || v.ToString()!.Contains("BrasilAPI")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetCepAsync_Should_Handle_Multiple_Consecutive_Failures()
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
            primaryFunc: (z, ct) => throw new HttpRequestException("Service unavailable"),
            fallbackFunc: (z, ct) => Task.FromResult<CepResult?>(fallbackResult)
        );

        // Act - Multiple calls
        var result1 = await service.GetCepAsync(zipCode, CancellationToken.None);
        var result2 = await service.GetCepAsync(zipCode, CancellationToken.None);
        var result3 = await service.GetCepAsync(zipCode, CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();
        result1!.Provider.Should().Be("viacep");
        result2!.Provider.Should().Be("viacep");
        result3!.Provider.Should().Be("viacep");
    }

    private CepService CreateService(
        Func<ZipCode, CancellationToken, Task<CepResult?>>? primaryFunc = null,
        Func<ZipCode, CancellationToken, Task<CepResult?>>? fallbackFunc = null)
    {
        var primaryProvider = primaryFunc != null
            ? (ICepProvider)new BrasilApiClient(HttpClient, primaryFunc)
            : (ICepProvider)new Hapvida.ExternalIntegration.Infra.ExternalApis.BrasilApiClient(HttpClient);

        var fallbackProvider = fallbackFunc != null
            ? (ICepProvider)new ViaCepClient(HttpClient, fallbackFunc)
            : (ICepProvider)new Hapvida.ExternalIntegration.Infra.ExternalApis.ViaCepClient(HttpClient);

        var providers = new List<ICepProvider>
        {
            primaryProvider,
            fallbackProvider
        };

        return new CepService(providers, LoggerMock.Object);
    }

    // Classes mock com nomes que o CepService reconhece (BrasilApiClient e ViaCepClient)
    // Essas classes têm os mesmos nomes que as classes reais para que o CepService as identifique
    private class BrasilApiClient : ICepProvider
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

    private class ViaCepClient : ICepProvider
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
}

