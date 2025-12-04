namespace Hapvida.ExternalIntegration.Application.Interfaces;

public interface IWeatherProvider
{
    Task<ForecastResponse?> GetForecastAsync(double latitude, double longitude, int days, CancellationToken cancellationToken = default);
    Task<GeocodingResponse?> GeocodeAsync(string city, string state, CancellationToken cancellationToken = default);
}

public class ForecastResponse
{
    public CurrentWeather? Current { get; set; }
    public DailyWeather? Daily { get; set; }
}

public class CurrentWeather
{
    public List<double> Temperature2m { get; set; } = new();
    public List<double> RelativeHumidity2m { get; set; } = new();
    public List<double> ApparentTemperature { get; set; } = new();
    public List<string> Time { get; set; } = new();
}

public class DailyWeather
{
    public List<string> Time { get; set; } = new();
    public List<double> Temperature2mMax { get; set; } = new();
    public List<double> Temperature2mMin { get; set; } = new();
}

public class GeocodingResponse
{
    public List<GeocodingResult> Results { get; set; } = new();
}

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Admin1 { get; set; } = string.Empty; // State
}

