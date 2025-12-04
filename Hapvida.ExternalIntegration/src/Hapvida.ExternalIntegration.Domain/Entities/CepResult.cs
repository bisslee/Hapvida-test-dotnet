using Hapvida.ExternalIntegration.Domain.ValueObjects;

namespace Hapvida.ExternalIntegration.Domain.Entities;

public class CepResult
{
    public ZipCode ZipCode { get; set; } = null!;
    public string? Street { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string? Ibge { get; set; }
    public Location? Location { get; set; }
    public string Provider { get; set; } = null!; // "brasilapi" ou "viacep"
}

