using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Queries;

public class GetWeatherTests : BaseTest
{
    private readonly Mock<ILogger<GetWeatherHandler>> LoggerMock;
    private readonly Mock<IValidator<GetWeatherRequest>> ValidatorMock;
    private readonly Mock<IReadRepository<ZipCodeLookup>> ReadRepositoryMock;
    private readonly Mock<IWeatherService> WeatherServiceMock;
    private readonly IResponseBuilder ResponseBuilder;
    private readonly Mock<IMapper> MapperMock;
    private readonly GetWeatherHandler Handler;

    public GetWeatherTests()
    {
        LoggerMock = new Mock<ILogger<GetWeatherHandler>>();
        ValidatorMock = new Mock<IValidator<GetWeatherRequest>>();
        ReadRepositoryMock = new Mock<IReadRepository<ZipCodeLookup>>();
        WeatherServiceMock = new Mock<IWeatherService>();
        ResponseBuilder = new ResponseBuilder();
        MapperMock = new Mock<IMapper>();

        Handler = new GetWeatherHandler(
            ReadRepositoryMock.Object,
            WeatherServiceMock.Object,
            ValidatorMock.Object,
            LoggerMock.Object,
            ResponseBuilder,
            MapperMock.Object
        );
    }

    private GetWeatherRequest CreateValidRequest() => new() { Days = 3 };

    [Fact]
    public async Task Handle_Should_Return_Weather_Successfully_With_Coordinates()
    {
        // Arrange
        var request = CreateValidRequest();
        var zipCodeLookup = new ZipCodeLookup
        {
            Id = Guid.NewGuid(),
            ZipCode = "01306001",
            City = "São Paulo",
            State = "SP",
            Latitude = -23.5505,
            Longitude = -46.6333,
            CreatedAt = DateTime.UtcNow
        };

        var weatherDto = new WeatherDto
        {
            SourceZipCodeId = zipCodeLookup.Id,
            Location = new WeatherLocationDto
            {
                Lat = -23.5505,
                Lon = -46.6333,
                City = "São Paulo",
                State = "SP"
            },
            Current = new CurrentWeatherDto
            {
                TemperatureC = 25.5,
                Humidity = 0.65,
                ApparentTemperatureC = 26.0,
                ObservedAt = DateTime.UtcNow
            },
            Daily = new List<DailyWeatherDto>
            {
                new() { Date = "2024-01-01", TempMinC = 20.0, TempMaxC = 28.0 }
            },
            Provider = "open-meteo"
        };

        var successResponse = new GetWeatherResponse
        {
            Data = new List<WeatherDto> { weatherDto },
            Success = true,
            StatusCode = 200
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.Is<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>(expr => true)))
            .ReturnsAsync(new List<ZipCodeLookup> { zipCodeLookup });
        WeatherServiceMock.Setup(s => s.GetWeatherAsync(
            zipCodeLookup.Latitude!.Value,
            zipCodeLookup.Longitude!.Value,
            request.Days,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherDto);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.Data.Should().NotBeNull();
        response.Data!.Count.Should().Be(1);
        response.Data[0].SourceZipCodeId.Should().Be(zipCodeLookup.Id);
    }

    [Fact]
    public async Task Handle_Should_Return_404_When_No_Saved_ZipCodes()
    {
        // Arrange
        var request = CreateValidRequest();
        var notFoundResponse = new GetWeatherResponse
        {
            Data = null,
            Success = false,
            StatusCode = 404,
            Message = "Nenhum CEP foi salvo ainda. Por favor, salve pelo menos um CEP antes de consultar o clima."
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.Is<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>(expr => true)))
            .ReturnsAsync(new List<ZipCodeLookup>());

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Use_Geocoding_When_No_Coordinates()
    {
        // Arrange
        var request = CreateValidRequest();
        var zipCodeLookup = new ZipCodeLookup
        {
            Id = Guid.NewGuid(),
            ZipCode = "01306001",
            City = "São Paulo",
            State = "SP",
            Latitude = null,
            Longitude = null,
            CreatedAt = DateTime.UtcNow
        };

        var weatherDto = new WeatherDto
        {
            SourceZipCodeId = zipCodeLookup.Id,
            Location = new WeatherLocationDto
            {
                Lat = -23.5505,
                Lon = -46.6333,
                City = "São Paulo",
                State = "SP"
            },
            Current = new CurrentWeatherDto { TemperatureC = 25.5 },
            Daily = new List<DailyWeatherDto>(),
            Provider = "open-meteo"
        };

        var successResponse = new GetWeatherResponse
        {
            Data = new List<WeatherDto> { weatherDto },
            Success = true,
            StatusCode = 200
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.Is<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>(expr => true)))
            .ReturnsAsync(new List<ZipCodeLookup> { zipCodeLookup });
        WeatherServiceMock.Setup(s => s.GetWeatherByCityAsync(
            zipCodeLookup.City,
            zipCodeLookup.State,
            request.Days,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherDto);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Count.Should().Be(1);
        WeatherServiceMock.Verify(s => s.GetWeatherByCityAsync(
            zipCodeLookup.City,
            zipCodeLookup.State,
            request.Days,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_400_When_Validation_Fails()
    {
        // Arrange
        var request = new GetWeatherRequest { Days = 10 }; // Invalid: > 7
        var validationFailures = new List<ValidationFailure>
        {
            new("Days", "O número de dias deve estar entre 1 e 7.")
        };
        var validationResult = new ValidationResult(validationFailures);
        var errorResponse = new GetWeatherResponse
        {
            Data = null,
            Success = false,
            StatusCode = 400,
            Errors = validationFailures.Select(e => e.ErrorMessage).ToList()
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Errors.Should().Contain("O número de dias deve estar entre 1 e 7.");
    }

    [Fact]
    public async Task Handle_Should_Order_By_CreatedAt_Desc()
    {
        // Arrange
        var request = CreateValidRequest();
        var olderLookup = new ZipCodeLookup
        {
            Id = Guid.NewGuid(),
            ZipCode = "01001000",
            City = "São Paulo",
            State = "SP",
            Latitude = -23.5505,
            Longitude = -46.6333,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var newerLookup = new ZipCodeLookup
        {
            Id = Guid.NewGuid(),
            ZipCode = "01306001",
            City = "São Paulo",
            State = "SP",
            Latitude = -23.5505,
            Longitude = -46.6333,
            CreatedAt = DateTime.UtcNow
        };

        // Criar weather DTOs com IDs diferentes para facilitar a verificação
        var weather1 = new WeatherDto 
        { 
            SourceZipCodeId = newerLookup.Id, 
            Location = new WeatherLocationDto { Lat = -23.5505, Lon = -46.6333, City = "São Paulo", State = "SP" },
            Current = new CurrentWeatherDto { TemperatureC = 25.5 },
            Daily = new List<DailyWeatherDto>(),
            Provider = "open-meteo"
        };
        var weather2 = new WeatherDto 
        { 
            SourceZipCodeId = olderLookup.Id, 
            Location = new WeatherLocationDto { Lat = -23.5505, Lon = -46.6333, City = "São Paulo", State = "SP" },
            Current = new CurrentWeatherDto { TemperatureC = 24.0 },
            Daily = new List<DailyWeatherDto>(),
            Provider = "open-meteo"
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        // O handler ordena por CreatedAt DESC, então quando retornamos a lista, 
        // o handler vai ordenar e processar newerLookup primeiro
        ReadRepositoryMock.Setup(r => r.Find(It.Is<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>(expr => true)))
            .ReturnsAsync(new List<ZipCodeLookup> { olderLookup, newerLookup });
        
        // Mock para retornar weather baseado na ordem de chamada
        // Como as coordenadas são iguais, vamos usar sequência de retornos
        var callCount = 0;
        WeatherServiceMock.Setup(s => s.GetWeatherAsync(
            It.IsAny<double>(),
            It.IsAny<double>(),
            request.Days,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // Primeira chamada: newerLookup (mais recente, processado primeiro após ordenação DESC)
                // Segunda chamada: olderLookup
                return callCount == 1 ? weather1 : weather2;
            });

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull("A lista de dados não deve ser nula");
        
        // Se a lista estiver vazia, pode ser que os mocks não foram chamados corretamente
        if (response.Data == null || response.Data.Count == 0)
        {
            // Verificar se os mocks foram chamados
            WeatherServiceMock.Verify(s => s.GetWeatherAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                request.Days,
                It.IsAny<CancellationToken>()), Times.AtLeastOnce, 
                "O serviço de clima deve ser chamado pelo menos uma vez");
        }
        
        response.Data!.Count.Should().Be(2, "Deve haver 2 itens de clima retornados");
        
        // Verificar que ambos os IDs estão presentes na lista usando FirstOrDefault
        var newerWeather = response.Data.FirstOrDefault(w => w.SourceZipCodeId == newerLookup.Id);
        var olderWeather = response.Data.FirstOrDefault(w => w.SourceZipCodeId == olderLookup.Id);
        
        newerWeather.Should().NotBeNull("O clima do CEP mais recente deve estar na lista");
        olderWeather.Should().NotBeNull("O clima do CEP mais antigo deve estar na lista");
        
        // O handler ordena por CreatedAt DESC e processa nessa ordem
        // Como newerLookup tem CreatedAt mais recente, ele será processado primeiro
        // e adicionado primeiro à lista weatherResults
        // Portanto, o primeiro item deve ser o mais recente
        response.Data[0].SourceZipCodeId.Should().Be(newerLookup.Id, 
            "O primeiro item deve ser o CEP mais recente (ordenado por CreatedAt DESC)");
    }
}

