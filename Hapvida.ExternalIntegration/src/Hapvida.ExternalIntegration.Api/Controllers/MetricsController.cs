using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using Swashbuckle.AspNetCore.Annotations;

namespace Hapvida.ExternalIntegration.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Métricas da API")]
public class MetricsController : ControllerBase
{
    private static readonly Meter Meter = new("Hapvida.ExternalIntegration.Api");

    [HttpGet]
    [SwaggerOperation(
        Summary = "Obter métricas da API",
        Description = "Retorna informações sobre as métricas coletadas pela API (requisições por rota, latência, etc.)",
        OperationId = "GetMetrics",
        Tags = new[] { "Metrics" }
    )]
    [SwaggerResponse(200, "Métricas obtidas com sucesso")]
    public IActionResult Get()
    {
        // Nota: Em produção, você pode usar OpenTelemetry ou Prometheus para expor métricas
        // Por enquanto, retornamos informações básicas sobre as métricas disponíveis
        return Ok(new
        {
            message = "Métricas estão sendo coletadas via System.Diagnostics.Metrics",
            availableMetrics = new[]
            {
                "http_requests_total - Total de requisições HTTP (contador)",
                "http_request_duration_seconds - Duração das requisições HTTP (histograma)"
            },
            note = "Use ferramentas como OpenTelemetry ou Prometheus para visualizar as métricas em tempo real"
        });
    }
}

