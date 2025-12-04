using Hapvida.ExternalIntegration.Api.Helper;
using Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;
using Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Hapvida.ExternalIntegration.Api.Controllers;

/// <summary>
/// Controller responsável por consultar informações de CEP
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Consulta de CEP")]
public class CepController : BaseControllerHandle
{
    private readonly IMediator _mediator;
    private readonly ILogger<CepController> _logger;

    public CepController(
        IMediator mediator,
        ILogger<CepController> logger) : base()
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Consulta um CEP e retorna o endereço normalizado
    /// </summary>
    /// <param name="zipCode">CEP com ou sem hífen (ex: 01001000 ou 01001-000)</param>
    /// <returns>Endereço completo do CEP</returns>
    /// <response code="200">CEP encontrado</response>
    /// <response code="400">CEP inválido</response>
    /// <response code="404">CEP não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet("{zipCode}")]
    [SwaggerOperation(
        Summary = "Consultar CEP",
        Description = "Consulta um CEP e retorna o endereço normalizado. Aceita CEPs com ou sem hífen. Utiliza BrasilAPI como provedor primário e ViaCEP como fallback.",
        OperationId = "GetCep",
        Tags = new[] { "CEP" }
    )]
    [SwaggerResponse(200, "CEP encontrado e retornado com sucesso", typeof(GetCepResponse))]
    [SwaggerResponse(400, "CEP inválido", typeof(GetCepResponse))]
    [SwaggerResponse(404, "CEP não encontrado", typeof(GetCepResponse))]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(GetCepResponse))]
    public async Task<ActionResult> Get(string zipCode, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/cep/{ZipCode} - Starting CEP consultation request", zipCode);

        try
        {
            var request = new GetCepRequest
            {
                ZipCode = zipCode
            };

            var response = await _mediator.Send(request, cancellationToken);
            _logger.LogInformation("GET /api/v1/cep/{ZipCode} - Successfully processed CEP consultation request. StatusCode: {StatusCode}, Provider: {Provider}",
                zipCode, response.StatusCode, response.Data?.Provider);
            return HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/v1/cep/{ZipCode} - Unexpected error occurred while processing CEP consultation request", zipCode);
            return HandleException(ex);
        }
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Persistir CEP",
        Description = "Consulta um CEP e persiste o resultado no banco de dados em memória. Reutiliza a lógica da consulta de CEP (US01). Não permite entradas repetidas.",
        OperationId = "AddZipCodeLookup",
        Tags = new[] { "CEP" }
    )]
    [SwaggerResponse(201, "CEP persistido com sucesso", typeof(AddZipCodeLookupResponse))]
    [SwaggerResponse(400, "CEP inválido", typeof(AddZipCodeLookupResponse))]
    [SwaggerResponse(404, "CEP não encontrado", typeof(AddZipCodeLookupResponse))]
    [SwaggerResponse(409, "CEP já está persistido no banco de dados", typeof(AddZipCodeLookupResponse))]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(AddZipCodeLookupResponse))]
    public async Task<ActionResult> Post([FromBody] AddZipCodeLookupRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/cep - Starting CEP persistence request. ZipCode: {ZipCode}", request.ZipCode);

        try
        {
            var response = await _mediator.Send(request, cancellationToken);
            _logger.LogInformation("POST /api/v1/cep - Successfully processed CEP persistence request. StatusCode: {StatusCode}, ZipCode: {ZipCode}",
                response.StatusCode, request.ZipCode);
            return HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST /api/v1/cep - Unexpected error occurred while processing CEP persistence request. ZipCode: {ZipCode}", request.ZipCode);
            return HandleException(ex);
        }
    }
}
