using System.Net.Http.Json;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Biss.MultiSinkLogger;

namespace Hapvida.ExternalIntegration.Infra.ExternalApis;

public class ViaCepClient : ICepProvider
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://viacep.com.br/ws";

    public ViaCepClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CepResult?> GetCepAsync(ZipCode zipCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl}/{zipCode.Value}/json/";
            Logger.Info($"Consultando ViaCEP para CEP: {zipCode.Value}");

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var viaCepResponse = await response.Content.ReadFromJsonAsync<ViaCepResponse>(cancellationToken: cancellationToken);

            if (viaCepResponse == null || viaCepResponse.Erro)
            {
                Logger.Warning($"CEP {zipCode.Value} não encontrado na ViaCEP");
                return null;
            }

            Logger.Info($"CEP {zipCode.Value} encontrado na ViaCEP");

            return new CepResult
            {
                ZipCode = zipCode,
                Street = viaCepResponse.Logradouro,
                District = viaCepResponse.Bairro,
                City = viaCepResponse.Localidade,
                State = viaCepResponse.Uf,
                Ibge = viaCepResponse.Ibge,
                Location = null, // ViaCEP não retorna coordenadas
                Provider = "viacep"
            };
        }
        catch (HttpRequestException ex)
        {
            Logger.Error($"Erro ao consultar ViaCEP para CEP: {zipCode.Value}. Erro: {ex.Message}. Tentativas de retry e circuit breaker podem ter sido aplicadas.");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Logger.Error($"Timeout ao consultar ViaCEP para CEP: {zipCode.Value}. Timeout após tentativas de retry.");
            throw;
        }
    }

    private class ViaCepResponse
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; }
        public string? Uf { get; set; }
        public string? Ibge { get; set; }
        public bool Erro { get; set; }
    }
}

