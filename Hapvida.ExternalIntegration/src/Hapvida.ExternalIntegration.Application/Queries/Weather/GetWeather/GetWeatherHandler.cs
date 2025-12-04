using AutoMapper;
using FluentValidation;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.Domain.Resources;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;

public class GetWeatherHandler : IRequestHandler<GetWeatherRequest, GetWeatherResponse>
{
    private readonly ILogger<GetWeatherHandler> Logger;
    private readonly IValidator<GetWeatherRequest> Validator;
    private readonly IReadRepository<ZipCodeLookup> ReadRepository;
    private readonly IWeatherService WeatherService;
    private readonly IResponseBuilder ResponseBuilder;
    private readonly IMapper Mapper;

    public GetWeatherHandler(
        IReadRepository<ZipCodeLookup> readRepository,
        IWeatherService weatherService,
        IValidator<GetWeatherRequest> validator,
        ILogger<GetWeatherHandler> logger,
        IResponseBuilder responseBuilder,
        IMapper mapper)
    {
        ReadRepository = readRepository;
        WeatherService = weatherService;
        Validator = validator;
        Logger = logger;
        ResponseBuilder = responseBuilder;
        Mapper = mapper;
    }

    public async Task<GetWeatherResponse> Handle(GetWeatherRequest request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting GetWeatherRequest processing. Days: {Days}", request.Days);

        var validationResult = await Validator.ValidateAsync(request, cancellationToken);
        if (validationResult != null && !validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            Logger.LogWarning("Validation failed for GetWeatherRequest. Days: {Days}, Errors: {Errors}",
                request.Days, string.Join(", ", errors));
            return ResponseBuilder.BuildValidationErrorResponse<GetWeatherResponse, List<WeatherDto>>(
                "Validação falhou", errors);
        }

        try
        {
            // Buscar todos os CEPs salvos ordenados por CreatedAt DESC
            Logger.LogDebug("Buscando CEPs salvos no banco de dados");
            var zipCodeLookups = await ReadRepository.Find(x => true);
            
            if (zipCodeLookups == null || zipCodeLookups.Count == 0)
            {
                Logger.LogWarning("Nenhum CEP persistido encontrado no banco de dados");
                return ResponseBuilder.BuildNotFoundResponse<GetWeatherResponse, List<WeatherDto>>(
                    "Nenhum CEP foi salvo ainda. Por favor, salve pelo menos um CEP antes de consultar o clima.");
            }

            // Ordenar por CreatedAt DESC
            var sortedLookups = zipCodeLookups.OrderByDescending(x => x.CreatedAt).ToList();
            Logger.LogInformation("Encontrados {Count} CEPs salvos", sortedLookups.Count);

            var weatherResults = new List<WeatherDto>();

            foreach (var lookup in sortedLookups)
            {
                WeatherDto? weather = null;

                // Se tiver Lat/Lon, usar diretamente
                if (lookup.Latitude.HasValue && lookup.Longitude.HasValue)
                {
                    Logger.LogDebug("Usando coordenadas do CEP salvo. ZipCode: {ZipCode}, Lat: {Latitude}, Lon: {Longitude}",
                        lookup.ZipCode, lookup.Latitude.Value, lookup.Longitude.Value);
                    weather = await WeatherService.GetWeatherAsync(
                        lookup.Latitude.Value,
                        lookup.Longitude.Value,
                        request.Days,
                        cancellationToken);
                }
                else
                {
                    // Geocodificar por city + state
                    Logger.LogDebug("Geocodificando cidade e estado. City: {City}, State: {State}",
                        lookup.City, lookup.State);
                    weather = await WeatherService.GetWeatherByCityAsync(
                        lookup.City,
                        lookup.State,
                        request.Days,
                        cancellationToken);
                }

                if (weather != null)
                {
                    weather.SourceZipCodeId = lookup.Id;
                    if (weather.Location != null)
                    {
                        weather.Location.City = lookup.City;
                        weather.Location.State = lookup.State;
                    }
                    weatherResults.Add(weather);
                }
                else
                {
                    Logger.LogWarning("Não foi possível obter weather para CEP: {ZipCode}", lookup.ZipCode);
                }
            }

            if (weatherResults.Count == 0)
            {
                Logger.LogWarning("Nenhum weather foi obtido para os CEPs salvos");
                return ResponseBuilder.BuildErrorResponse<GetWeatherResponse, List<WeatherDto>>(
                    "Não foi possível obter informações de clima para os CEPs salvos",
                    new[] { "Não foi possível obter informações de clima para os CEPs salvos" },
                    500);
            }

            Logger.LogInformation("Successfully retrieved weather for {Count} locations", weatherResults.Count);
            return ResponseBuilder.BuildSuccessResponse<GetWeatherResponse, List<WeatherDto>>(weatherResults);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred while retrieving weather. Days: {Days}", request.Days);
            throw; // Deixar o middleware global tratar a exceção
        }
    }
}

