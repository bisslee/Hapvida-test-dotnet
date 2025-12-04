using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hapvida.ExternalIntegration.Api.Helpers;

public static class ProblemDetailsHelper
{
    private const string BaseErrorUri = "https://errors.hapvida.externalintegration";

    public static ProblemDetails CreateInvalidCepProblemDetails(HttpContext context, string detail)
    {
        return new ProblemDetails
        {
            Type = $"{BaseErrorUri}/invalid-cep",
            Title = "CEP inválido",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };
    }

    public static ProblemDetails CreateCepNotFoundProblemDetails(HttpContext context, string zipCode)
    {
        return new ProblemDetails
        {
            Type = $"{BaseErrorUri}/cep-not-found",
            Title = "CEP não encontrado",
            Status = StatusCodes.Status404NotFound,
            Detail = $"O CEP '{zipCode}' não foi encontrado em nenhum provedor externo.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier,
                ["zipCode"] = zipCode
            }
        };
    }

    public static ProblemDetails CreateNoSavedCepProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = $"{BaseErrorUri}/no-saved-cep",
            Title = "Nenhum CEP salvo",
            Status = StatusCodes.Status404NotFound,
            Detail = "Nenhum CEP foi salvo no banco de dados. Por favor, salve um CEP antes de consultar o clima.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };
    }

    public static ProblemDetails CreateInvalidDaysParameterProblemDetails(HttpContext context, int? days)
    {
        return new ProblemDetails
        {
            Type = $"{BaseErrorUri}/invalid-days-parameter",
            Title = "Parâmetro days inválido",
            Status = StatusCodes.Status400BadRequest,
            Detail = $"O parâmetro 'days' deve estar entre 1 e 7. Valor recebido: {days}",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier,
                ["days"] = days
            }
        };
    }

    public static ProblemDetails CreateTimeoutProblemDetails(HttpContext context, string provider)
    {
        return new ProblemDetails
        {
            Type = $"{BaseErrorUri}/timeout",
            Title = "Timeout",
            Status = StatusCodes.Status504GatewayTimeout,
            Detail = $"A requisição ao provedor externo '{provider}' excedeu o tempo limite.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier,
                ["provider"] = provider
            }
        };
    }

    public static ProblemDetails CreateConflictProblemDetails(HttpContext context, string detail)
    {
        return new ProblemDetails
        {
            Type = $"{BaseErrorUri}/conflict",
            Title = "Conflito",
            Status = StatusCodes.Status409Conflict,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };
    }
}

