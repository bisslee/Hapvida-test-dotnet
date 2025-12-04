namespace Hapvida.ExternalIntegration.Application.Models;

public class WeatherDto
{
    public Guid SourceZipCodeId { get; set; }
    public WeatherLocationDto Location { get; set; } = null!;
    public CurrentWeatherDto Current { get; set; } = null!;
    public List<DailyWeatherDto> Daily { get; set; } = new();
    public string Provider { get; set; } = "open-meteo";
}

public class WeatherLocationDto
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class CurrentWeatherDto
{
    public double TemperatureC { get; set; }
    public double Humidity { get; set; }
    public double ApparentTemperatureC { get; set; }
    public DateTime ObservedAt { get; set; }
}

public class DailyWeatherDto
{
    public string Date { get; set; } = null!; // ISO 8601 format
    public double TempMinC { get; set; }
    public double TempMaxC { get; set; }
}

