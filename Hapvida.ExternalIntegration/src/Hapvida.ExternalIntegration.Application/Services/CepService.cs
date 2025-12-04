using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Biss.MultiSinkLogger;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.Application.Services;

public class CepService : ICepService
{
    private readonly ICepProvider _primaryProvider; // BrasilAPI
    private readonly ICepProvider _fallbackProvider; // ViaCEP
    private readonly ILogger<CepService> _logger;

    public CepService(
        IEnumerable<ICepProvider> providers,
        ILogger<CepService> logger)
    {
        var providerList = providers.ToList();
        
        // Identifica providers pelo nome do tipo (BrasilApiClient e ViaCepClient)
        _primaryProvider = providerList.FirstOrDefault(p => 
            p.GetType().Name.Equals("BrasilApiClient", StringComparison.OrdinalIgnoreCase)) 
            ?? throw new InvalidOperationException("BrasilAPI provider not found");
            
        _fallbackProvider = providerList.FirstOrDefault(p => 
            p.GetType().Name.Equals("ViaCepClient", StringComparison.OrdinalIgnoreCase)) 
            ?? throw new InvalidOperationException("ViaCEP provider not found");

        _logger = logger;
    }

    public async Task<CepResult?> GetCepAsync(ZipCode zipCode, CancellationToken cancellationToken = default)
    {
        // Tenta primeiro com BrasilAPI (primária)
        var result = await TryGetFromBrasilApiAsync(zipCode, cancellationToken);
        if (result != null)
        {
            return result;
        }

        // Fallback para ViaCEP
        result = await TryGetFromViaCepAsync(zipCode, cancellationToken);
        return result;
    }

    private async Task<CepResult?> TryGetFromBrasilApiAsync(ZipCode zipCode, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Tentando consultar CEP {ZipCode} na BrasilAPI (primária)", zipCode.Value);
            var result = await _primaryProvider.GetCepAsync(zipCode, cancellationToken);
            
            if (result != null)
            {
                _logger.LogInformation("CEP {ZipCode} encontrado na BrasilAPI", zipCode.Value);
                return result;
            }

            _logger.LogWarning("CEP {ZipCode} não encontrado na BrasilAPI", zipCode.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar BrasilAPI para CEP {ZipCode}, tentando fallback ViaCEP", zipCode.Value);
            return null;
        }
    }

    private async Task<CepResult?> TryGetFromViaCepAsync(ZipCode zipCode, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Tentando consultar CEP {ZipCode} na ViaCEP (fallback)", zipCode.Value);
            var result = await _fallbackProvider.GetCepAsync(zipCode, cancellationToken);
            
            if (result != null)
            {
                _logger.LogInformation("CEP {ZipCode} encontrado na ViaCEP", zipCode.Value);
                return result;
            }

            _logger.LogWarning("CEP {ZipCode} não encontrado na ViaCEP", zipCode.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar ViaCEP para CEP {ZipCode}", zipCode.Value);
            throw;
        }
    }
}
