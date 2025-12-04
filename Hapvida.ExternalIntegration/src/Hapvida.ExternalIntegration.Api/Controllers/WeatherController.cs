using Hapvida.ExternalIntegration.Api.Helper;
using Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Hapvida.ExternalIntegration.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Consulta de Clima")]
public class WeatherController : BaseControllerHandle
{
    private readonly IMediator _mediator;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IMediator mediator,
        ILogger<WeatherController> logger) : base()
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Consultar Clima",
        Description = "Consulta o clima atual e previsão para N dias usando os CEPs salvos no banco de dados. Se não houver CEPs salvos, retorna 404. Usa Lat/Lon dos CEPs salvos ou geocodifica por cidade/estado se não houver coordenadas.",
        OperationId = "GetWeather",
        Tags = new[] { "Weather" }
    )]
    [SwaggerResponse(200, "Clima obtido com sucesso", typeof(GetWeatherResponse))]
    [SwaggerResponse(400, "Parâmetro days inválido (deve estar entre 1 e 7)", typeof(GetWeatherResponse))]
    [SwaggerResponse(404, "Nenhum CEP salvo encontrado", typeof(GetWeatherResponse))]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(GetWeatherResponse))]
    public async Task<ActionResult> Get([FromQuery] int? days = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/v1/weather?days={Days} - Starting weather consultation request", days);

        try
        {
            var request = new GetWeatherRequest
            {
                Days = days ?? 3 // Default to 3 if not provided
            };

            var response = await _mediator.Send(request, cancellationToken);
            _logger.LogInformation("GET /api/v1/weather?days={Days} - Successfully processed weather consultation request. StatusCode: {StatusCode}, Count: {Count}",
                days, response.StatusCode, response.Data?.Count ?? 0);
            return HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/v1/weather?days={Days} - Unexpected error occurred while processing weather consultation request", days);
            return HandleException(ex);
        }
    }
}

