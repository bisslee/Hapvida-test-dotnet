using FluentAssertions;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Application.Services;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Services;

public class WeatherServiceTests : BaseTest
{
    private readonly Mock<IWeatherProvider> WeatherProviderMock;
    private readonly IMemoryCache MemoryCache;
    private readonly Mock<ILogger<WeatherService>> LoggerMock;
    private readonly WeatherService Service;

    public WeatherServiceTests()
    {
        WeatherProviderMock = new Mock<IWeatherProvider>();
        MemoryCache = new MemoryCache(new MemoryCacheOptions());
        LoggerMock = new Mock<ILogger<WeatherService>>();
        Service = new WeatherService(
            WeatherProviderMock.Object,
            MemoryCache,
            LoggerMock.Object
        );
    }

    [Fact]
    public async Task GetWeatherAsync_Should_Return_Weather_From_Provider()
    {
        // Arrange
        var latitude = -23.5505;
        var longitude = -46.6333;
        var days = 3;

        var forecastResponse = new ForecastResponse
        {
            Current = new CurrentWeather
            {
                Temperature2m = new List<double> { 25.5 },
                RelativeHumidity2m = new List<double> { 65 },
                ApparentTemperature = new List<double> { 26.0 },
                Time = new List<string> { DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm") }
            },
            Daily = new DailyWeather
            {
                Time = new List<string> { "2024-01-01", "2024-01-02", "2024-01-03" },
                Temperature2mMax = new List<double> { 28.0, 29.0, 30.0 },
                Temperature2mMin = new List<double> { 20.0, 21.0, 22.0 }
            }
        };

        WeatherProviderMock.Setup(p => p.GetForecastAsync(latitude, longitude, days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecastResponse);

        // Act
        var result = await Service.GetWeatherAsync(latitude, longitude, days, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Location.Lat.Should().Be(latitude);
        result.Location.Lon.Should().Be(longitude);
        result.Current.TemperatureC.Should().Be(25.5);
        result.Current.Humidity.Should().Be(0.65); // 65% / 100
        result.Daily.Count.Should().Be(3);
        result.Provider.Should().Be("open-meteo");
    }

    [Fact]
    public async Task GetWeatherAsync_Should_Use_Cache_On_Second_Call()
    {
        // Arrange
        var latitude = -23.5505;
        var longitude = -46.6333;
        var days = 3;

        var forecastResponse = new ForecastResponse
        {
            Current = new CurrentWeather
            {
                Temperature2m = new List<double> { 25.5 },
                RelativeHumidity2m = new List<double> { 65 },
                ApparentTemperature = new List<double> { 26.0 },
                Time = new List<string> { DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm") }
            },
            Daily = new DailyWeather
            {
                Time = new List<string> { "2024-01-01" },
                Temperature2mMax = new List<double> { 28.0 },
                Temperature2mMin = new List<double> { 20.0 }
            }
        };

        WeatherProviderMock.Setup(p => p.GetForecastAsync(latitude, longitude, days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecastResponse);

        // Act - First call
        var result1 = await Service.GetWeatherAsync(latitude, longitude, days, CancellationToken.None);

        // Act - Second call (should use cache)
        var result2 = await Service.GetWeatherAsync(latitude, longitude, days, CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().BeSameAs(result2); // Same instance from cache
        WeatherProviderMock.Verify(p => p.GetForecastAsync(latitude, longitude, days, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherAsync_Should_Return_Null_When_Provider_Returns_Null()
    {
        // Arrange
        var latitude = -23.5505;
        var longitude = -46.6333;
        var days = 3;

        WeatherProviderMock.Setup(p => p.GetForecastAsync(latitude, longitude, days, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastResponse?)null);

        // Act
        var result = await Service.GetWeatherAsync(latitude, longitude, days, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherByCityAsync_Should_Geocode_And_Get_Weather()
    {
        // Arrange
        var city = "São Paulo";
        var state = "SP";
        var days = 3;
        var latitude = -23.5505;
        var longitude = -46.6333;

        var geocodingResponse = new GeocodingResponse
        {
            Results = new List<GeocodingResult>
            {
                new() { Latitude = latitude, Longitude = longitude, Name = city, Country = "Brazil", Admin1 = state }
            }
        };

        var forecastResponse = new ForecastResponse
        {
            Current = new CurrentWeather
            {
                Temperature2m = new List<double> { 25.5 },
                RelativeHumidity2m = new List<double> { 65 },
                ApparentTemperature = new List<double> { 26.0 },
                Time = new List<string> { DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm") }
            },
            Daily = new DailyWeather
            {
                Time = new List<string> { "2024-01-01" },
                Temperature2mMax = new List<double> { 28.0 },
                Temperature2mMin = new List<double> { 20.0 }
            }
        };

        WeatherProviderMock.Setup(p => p.GeocodeAsync(city, state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(geocodingResponse);
        WeatherProviderMock.Setup(p => p.GetForecastAsync(latitude, longitude, days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecastResponse);

        // Act
        var result = await Service.GetWeatherByCityAsync(city, state, days, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Location.City.Should().Be(city);
        result.Location.State.Should().Be(state);
        result.Location.Lat.Should().Be(latitude);
        result.Location.Lon.Should().Be(longitude);
    }

    [Fact]
    public async Task GetWeatherByCityAsync_Should_Return_Null_When_Geocoding_Fails()
    {
        // Arrange
        var city = "InvalidCity";
        var state = "XX";
        var days = 3;

        WeatherProviderMock.Setup(p => p.GeocodeAsync(city, state, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodingResponse?)null);

        // Act
        var result = await Service.GetWeatherByCityAsync(city, state, days, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        WeatherProviderMock.Verify(p => p.GetForecastAsync(It.IsAny<double>(), It.IsAny<double>(), days, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GeocodeAsync_Should_Return_Coordinates()
    {
        // Arrange
        var city = "São Paulo";
        var state = "SP";
        var latitude = -23.5505;
        var longitude = -46.6333;

        var geocodingResponse = new GeocodingResponse
        {
            Results = new List<GeocodingResult>
            {
                new() { Latitude = latitude, Longitude = longitude, Name = city, Country = "Brazil", Admin1 = state }
            }
        };

        WeatherProviderMock.Setup(p => p.GeocodeAsync(city, state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(geocodingResponse);

        // Act
        var result = await Service.GeocodeAsync(city, state, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Latitude.Should().Be(latitude);
        result.Value.Longitude.Should().Be(longitude);
    }

    [Fact]
    public async Task GeocodeAsync_Should_Return_Null_When_No_Results()
    {
        // Arrange
        var city = "InvalidCity";
        var state = "XX";

        WeatherProviderMock.Setup(p => p.GeocodeAsync(city, state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResponse { Results = new List<GeocodingResult>() });

        // Act
        var result = await Service.GeocodeAsync(city, state, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}

