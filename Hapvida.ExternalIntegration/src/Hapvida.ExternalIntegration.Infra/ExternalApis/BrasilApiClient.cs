using System.Net.Http.Json;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Biss.MultiSinkLogger;

namespace Hapvida.ExternalIntegration.Infra.ExternalApis;

public class BrasilApiClient : ICepProvider
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://brasilapi.com.br/api/cep/v2";

    public BrasilApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CepResult?> GetCepAsync(ZipCode zipCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl}/{zipCode.Value}";
            Logger.Info($"Consultando BrasilAPI para CEP: {zipCode.Value}");

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.Warning($"CEP {zipCode.Value} não encontrado na BrasilAPI");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var brasilApiResponse = await response.Content.ReadFromJsonAsync<BrasilApiResponse>(cancellationToken: cancellationToken);

            if (brasilApiResponse == null)
                return null;

            Logger.Info($"CEP {zipCode.Value} encontrado na BrasilAPI");

            return new CepResult
            {
                ZipCode = zipCode,
                Street = brasilApiResponse.Street,
                District = brasilApiResponse.Neighborhood,
                City = brasilApiResponse.City,
                State = brasilApiResponse.State,
                Ibge = brasilApiResponse.Ibge,
                Location = brasilApiResponse.Coordinates != null
                    ? new Location
                    {
                        Latitude = brasilApiResponse.Coordinates.Latitude,
                        Longitude = brasilApiResponse.Coordinates.Longitude
                    }
                    : null,
                Provider = "brasilapi"
            };
        }
        catch (HttpRequestException ex)
        {
            Logger.Error($"Erro ao consultar BrasilAPI para CEP: {zipCode.Value}. Erro: {ex.Message}. Tentativas de retry e circuit breaker podem ter sido aplicadas.");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Logger.Error($"Timeout ao consultar BrasilAPI para CEP: {zipCode.Value}. Timeout após tentativas de retry.");
            throw;
        }
    }

    private class BrasilApiResponse
    {
        public string? Street { get; set; }
        public string? Neighborhood { get; set; }
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? Ibge { get; set; }
        public CoordinatesDto? Coordinates { get; set; }
    }

    private class CoordinatesDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

