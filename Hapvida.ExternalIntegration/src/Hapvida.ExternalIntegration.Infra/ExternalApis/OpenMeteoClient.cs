using Hapvida.ExternalIntegration.Application.Interfaces;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.Infra.ExternalApis;

public class OpenMeteoClient : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoClient> _logger;
    private const string ForecastBaseUrl = "https://api.open-meteo.com/v1/forecast";
    private const string GeocodingBaseUrl = "https://geocoding-api.open-meteo.com/v1/search";

    public OpenMeteoClient(HttpClient httpClient, ILogger<OpenMeteoClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Application.Interfaces.ForecastResponse?> GetForecastAsync(double latitude, double longitude, int days, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{ForecastBaseUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature&daily=temperature_2m_max,temperature_2m_min&timezone=America/Sao_Paulo&forecast_days={days}";
            _logger.LogInformation("Consultando Open-Meteo Forecast para lat: {Latitude}, lon: {Longitude}, days: {Days}", latitude, longitude, days);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var forecastResponse = await response.Content.ReadFromJsonAsync<Application.Interfaces.ForecastResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Forecast obtido com sucesso da Open-Meteo");
            return forecastResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Open-Meteo Forecast para lat: {Latitude}, lon: {Longitude}", latitude, longitude);
            throw;
        }
    }

    public async Task<Application.Interfaces.GeocodingResponse?> GeocodeAsync(string city, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"{city}, {state}, Brazil";
            var url = $"{GeocodingBaseUrl}?name={Uri.EscapeDataString(query)}&count=1&language=pt";
            _logger.LogInformation("Consultando Open-Meteo Geocoding para cidade: {City}, estado: {State}", city, state);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geocodingResponse = await response.Content.ReadFromJsonAsync<Application.Interfaces.GeocodingResponse>(cancellationToken: cancellationToken);
            
            if (geocodingResponse?.Results == null || geocodingResponse.Results.Count == 0)
            {
                _logger.LogWarning("Nenhum resultado encontrado na geocodificação para {City}, {State}", city, state);
                return null;
            }

            _logger.LogInformation("Geocodificação obtida com sucesso: lat: {Latitude}, lon: {Longitude}", 
                geocodingResponse.Results[0].Latitude, geocodingResponse.Results[0].Longitude);
            return geocodingResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Open-Meteo Geocoding para cidade: {City}, estado: {State}", city, state);
            throw;
        }
    }
}

