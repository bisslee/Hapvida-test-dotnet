namespace Hapvida.ExternalIntegration.Application.Models;

public class ZipCodeLookupDto
{
    public Guid Id { get; set; }
    public string ZipCode { get; set; } = null!;
    public string? Street { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string? Ibge { get; set; }
    public LocationDto? Location { get; set; }
    public string Provider { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

