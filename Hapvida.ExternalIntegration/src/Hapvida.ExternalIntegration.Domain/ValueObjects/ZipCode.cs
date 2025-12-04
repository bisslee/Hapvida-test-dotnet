using Hapvida.ExternalIntegration.Domain.Resources;

namespace Hapvida.ExternalIntegration.Domain.ValueObjects;

public class ZipCode
{
    private const int RequiredLength = 8;
    
    public string Value { get; private set; }

    private ZipCode(string value)
    {
        Value = value;
    }

    public static ZipCode Create(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException(HapvidaExternalIntegrationResource.ZipCode_Empty, nameof(zipCode));

        // Remove hífen e espaços
        var normalized = zipCode.Replace("-", "").Replace(" ", "");

        // Valida se contém apenas dígitos
        if (!normalized.All(char.IsDigit))
            throw new ArgumentException(HapvidaExternalIntegrationResource.ZipCode_InvalidCharacters, nameof(zipCode));

        // Valida tamanho
        if (normalized.Length != RequiredLength)
            throw new ArgumentException(string.Format(HapvidaExternalIntegrationResource.ZipCode_InvalidLength, RequiredLength), nameof(zipCode));

        return new ZipCode(normalized);
    }

    public static bool TryCreate(string zipCode, out ZipCode? result)
    {
        result = null;
        try
        {
            result = Create(zipCode);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;
}

