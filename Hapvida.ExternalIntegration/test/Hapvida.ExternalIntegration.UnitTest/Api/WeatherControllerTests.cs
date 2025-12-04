using FluentAssertions;
using Hapvida.ExternalIntegration.Api.Controllers;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;
using Hapvida.ExternalIntegration.UnitTest;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Api;

public class WeatherControllerTests : BaseTest
{
    private readonly Mock<IMediator> MediatorMock;
    private readonly Mock<ILogger<WeatherController>> LoggerMock;
    private readonly WeatherController Controller;

    public WeatherControllerTests()
    {
        MediatorMock = new Mock<IMediator>();
        LoggerMock = new Mock<ILogger<WeatherController>>();
        Controller = new WeatherController(
            MediatorMock.Object,
            LoggerMock.Object);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Get_Should_Return_200_When_Weather_Found()
    {
        // Arrange
        var days = 3;
        var weatherDto = new WeatherDto
        {
            SourceZipCodeId = Guid.NewGuid(),
            Location = new WeatherLocationDto { Lat = -23.5505, Lon = -46.6333, City = "São Paulo", State = "SP" },
            Current = new CurrentWeatherDto { TemperatureC = 25.5 },
            Daily = new List<DailyWeatherDto>(),
            Provider = "open-meteo"
        };

        var response = new GetWeatherResponse
        {
            Success = true,
            StatusCode = 200,
            Data = new List<WeatherDto> { weatherDto }
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<GetWeatherRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(days, CancellationToken.None);

        // Assert
        // Quando Data é uma coleção não vazia, BaseControllerHandle retorna ObjectResult com StatusCode 206 (PartialContent)
        // Isso é o comportamento esperado do template para coleções
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(206); // PartialContent para coleções não vazias
        ((objectResult.Value as GetWeatherResponse)!.Data)!.Should().HaveCount(1);
    }

    [Fact]
    public async Task Get_Should_Return_404_When_No_Saved_ZipCodes()
    {
        // Arrange
        var days = 3;
        var response = new GetWeatherResponse
        {
            Success = false,
            StatusCode = 404,
            Data = null,
            Message = "Nenhum CEP foi salvo ainda."
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<GetWeatherRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(days, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        (result as ObjectResult)!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Get_Should_Return_400_When_Validation_Fails()
    {
        // Arrange
        var days = 10; // Invalid: > 7
        var response = new GetWeatherResponse
        {
            Success = false,
            StatusCode = 400,
            Data = null,
            Errors = new List<string> { "O número de dias deve estar entre 1 e 7." }
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<GetWeatherRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(days, CancellationToken.None);

        // Assert
        // BaseControllerHandle retorna ObjectResult com ProblemDetails para erros 400
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Get_Should_Use_Default_Days_When_Not_Provided()
    {
        // Arrange
        var response = new GetWeatherResponse
        {
            Success = true,
            StatusCode = 200,
            Data = new List<WeatherDto>()
        };

        MediatorMock.Setup(m => m.Send(
            It.Is<GetWeatherRequest>(r => r.Days == 3), // Default value
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(null, CancellationToken.None); // No days parameter

        // Assert
        // Quando Data é uma coleção vazia, BaseControllerHandle retorna NoContent()
        result.Should().BeOfType<NoContentResult>();
        MediatorMock.Verify(m => m.Send(
            It.Is<GetWeatherRequest>(r => r.Days == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_Should_Handle_Exception()
    {
        // Arrange
        var days = 3;
        var expectedException = new Exception("Internal server error");

        MediatorMock.Setup(m => m.Send(It.IsAny<GetWeatherRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        // O controller re-lança a exceção para o middleware tratar
        await Assert.ThrowsAsync<Exception>(
            () => Controller.Get(days, CancellationToken.None));
    }
}

