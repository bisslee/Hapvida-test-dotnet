namespace Hapvida.ExternalIntegration.Domain.Entities;

public class ZipCodeLookup : BaseEntity
{
    public string ZipCode { get; set; } = null!;
    public string? Street { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string? Ibge { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Provider { get; set; } = null!;
}

