using Hapvida.ExternalIntegration.Application.Models;

namespace Hapvida.ExternalIntegration.Application.Interfaces;

public interface IWeatherService
{
    Task<WeatherDto?> GetWeatherAsync(double latitude, double longitude, int days, CancellationToken cancellationToken = default);
    Task<WeatherDto?> GetWeatherByCityAsync(string city, string state, int days, CancellationToken cancellationToken = default);
    Task<(double Latitude, double Longitude)?> GeocodeAsync(string city, string state, CancellationToken cancellationToken = default);
}

