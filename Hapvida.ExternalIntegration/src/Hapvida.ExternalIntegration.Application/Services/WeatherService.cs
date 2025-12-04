using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.Application.Services;

public class WeatherService : IWeatherService
{
    private readonly IWeatherProvider _weatherProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private const int CacheExpirationMinutes = 10;

    public WeatherService(
        IWeatherProvider weatherProvider,
        IMemoryCache cache,
        ILogger<WeatherService> logger)
    {
        _weatherProvider = weatherProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherDto?> GetWeatherAsync(double latitude, double longitude, int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"weather:{latitude}:{longitude}:{days}";
        
        if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
        {
            _logger.LogInformation("Weather encontrado no cache para lat: {Latitude}, lon: {Longitude}, days: {Days}", latitude, longitude, days);
            return cachedWeather;
        }

        _logger.LogInformation("Consultando Open-Meteo para lat: {Latitude}, lon: {Longitude}, days: {Days}", latitude, longitude, days);
        var forecastResponse = await _weatherProvider.GetForecastAsync(latitude, longitude, days, cancellationToken);

        if (forecastResponse == null || forecastResponse.Current == null || forecastResponse.Daily == null)
        {
            _logger.LogWarning("Resposta inválida da Open-Meteo para lat: {Latitude}, lon: {Longitude}", latitude, longitude);
            return null;
        }

        var weatherDto = MapToWeatherDto(forecastResponse, latitude, longitude, days);

        // Armazenar no cache com TTL de 10 minutos
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        };
        _cache.Set(cacheKey, weatherDto, cacheOptions);

        _logger.LogInformation("Weather obtido e armazenado no cache para lat: {Latitude}, lon: {Longitude}, days: {Days}", latitude, longitude, days);
        return weatherDto;
    }

    public async Task<WeatherDto?> GetWeatherByCityAsync(string city, string state, int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"weather:{city}:{state}:{days}";
        
        if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
        {
            _logger.LogInformation("Weather encontrado no cache para cidade: {City}, estado: {State}, days: {Days}", city, state, days);
            return cachedWeather;
        }

        // Geocodificar primeiro
        var coordinates = await GeocodeAsync(city, state, cancellationToken);
        if (coordinates == null)
        {
            _logger.LogWarning("Não foi possível geocodificar cidade: {City}, estado: {State}", city, state);
            return null;
        }

        // Buscar weather usando coordenadas
        var weather = await GetWeatherAsync(coordinates.Value.Latitude, coordinates.Value.Longitude, days, cancellationToken);
        
        if (weather != null)
        {
            weather.Location.City = city;
            weather.Location.State = state;
        }

        return weather;
    }

    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string city, string state, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Geocodificando cidade: {City}, estado: {State}", city, state);
        var geocodingResponse = await _weatherProvider.GeocodeAsync(city, state, cancellationToken);

        if (geocodingResponse?.Results == null || geocodingResponse.Results.Count == 0)
        {
            _logger.LogWarning("Geocodificação não retornou resultados para cidade: {City}, estado: {State}", city, state);
            return null;
        }

        var result = geocodingResponse.Results[0];
        _logger.LogInformation("Geocodificação bem-sucedida: lat: {Latitude}, lon: {Longitude}", result.Latitude, result.Longitude);
        return (result.Latitude, result.Longitude);
    }

    private WeatherDto MapToWeatherDto(ForecastResponse forecastResponse, double latitude, double longitude, int days)
    {
        var current = forecastResponse.Current;
        var daily = forecastResponse.Daily;

        var currentWeather = new CurrentWeatherDto
        {
            TemperatureC = current.Temperature2m.Count > 0 ? current.Temperature2m[0] : 0,
            Humidity = current.RelativeHumidity2m.Count > 0 ? current.RelativeHumidity2m[0] / 100.0 : 0, // Converter de porcentagem para decimal
            ApparentTemperatureC = current.ApparentTemperature.Count > 0 ? current.ApparentTemperature[0] : 0,
            ObservedAt = current.Time.Count > 0 && DateTime.TryParse(current.Time[0], out var observedTime) 
                ? observedTime.ToUniversalTime() 
                : DateTime.UtcNow
        };

        var dailyWeather = new List<DailyWeatherDto>();
        for (int i = 0; i < Math.Min(days, daily.Time.Count); i++)
        {
            dailyWeather.Add(new DailyWeatherDto
            {
                Date = daily.Time[i],
                TempMinC = daily.Temperature2mMin.Count > i ? daily.Temperature2mMin[i] : 0,
                TempMaxC = daily.Temperature2mMax.Count > i ? daily.Temperature2mMax[i] : 0
            });
        }

        return new WeatherDto
        {
            Location = new WeatherLocationDto
            {
                Lat = latitude,
                Lon = longitude
            },
            Current = currentWeather,
            Daily = dailyWeather,
            Provider = "open-meteo"
        };
    }
}

