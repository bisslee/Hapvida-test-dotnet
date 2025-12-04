using Biss.MultiSinkLogger;
using Hapvida.ExternalIntegration.Api.Helpers;
using Hapvida.ExternalIntegration.Domain.Entities.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections;
using System.Globalization;
using System.Net;

namespace Hapvida.ExternalIntegration.Api.Helper;

public class BaseControllerHandle : ControllerBase
{
    const string TotalCountHeader = "X-Total-Count";
    const string NullResponseMessage = "Null response received.";
    const string BadRequestMessage = "Bad Request: {@Response}";
    const string PartialContentMessage = "Partial Content: {@Response}";
    const string NoContentMessage = "No Content: {@Response}";
    const string InternalServerErrorMessage = "An error occurred: {@Message}";

    public BaseControllerHandle()
    {
    }

    [NonAction] // Evita que o Swagger considere este método como uma ação
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var acceptLanguage = context.HttpContext.Request.Headers["Accept-Language"].FirstOrDefault();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            try
            {
                var culture = new CultureInfo(acceptLanguage);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
                Logger.Warning($"Invalid culture provided: {acceptLanguage}");
            }
        }
    }

    [NonAction] // Adicione este atributo também aqui
    public void OnActionExecuted(ActionExecutedContext context) { }

    public ActionResult HandleResponse<TEntityResponse>(BaseResponse<TEntityResponse> response)
    {
        if (response == null)
        {
            Logger.Info(NullResponseMessage);
            return BadRequest(NullResponseMessage);
        }

        // Verificar StatusCode primeiro para retornar o código HTTP correto
        if (response.StatusCode == (int)HttpStatusCode.NotFound)
        {
            Logger.Info($"Not Found: {response.Message}");
            var problemDetails = CreateProblemDetailsFromResponse(response, HttpContext);
            return new ObjectResult(problemDetails) 
            { 
                StatusCode = (int)HttpStatusCode.NotFound,
                ContentTypes = { "application/problem+json" }
            };
        }

        if (response.StatusCode == (int)HttpStatusCode.Conflict)
        {
            Logger.Info($"Conflict: {response.Message}");
            var problemDetails = CreateProblemDetailsFromResponse(response, HttpContext);
            return new ObjectResult(problemDetails) 
            { 
                StatusCode = (int)HttpStatusCode.Conflict,
                ContentTypes = { "application/problem+json" }
            };
        }

        if (!response.Success)
        {
            Logger.Info(BadRequestMessage, response.Message);
            var problemDetails = CreateProblemDetailsFromResponse(response, HttpContext);
            return new ObjectResult(problemDetails) 
            { 
                StatusCode = (int)HttpStatusCode.BadRequest,
                ContentTypes = { "application/problem+json" }
            };
        }

        if (response.Data == null)
        {
            Logger.Warning($"Invalid response: {response}");
            return NoContent();
        }

        if (response.Data is ICollection collection)
        {
            if (collection.Count == 0)
            {
                Logger.Info(NoContentMessage, response.Message);
                return NoContent();
            }

            Response.Headers.Append(TotalCountHeader, collection.Count.ToString());
            Logger.Info(PartialContentMessage, response.Message);
            return new ObjectResult(response) { StatusCode = (int)HttpStatusCode.PartialContent };
        }

        return response.StatusCode switch
        {
            (int)HttpStatusCode.Created => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.Created },
            (int)HttpStatusCode.NoContent => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.NoContent },
            (int)HttpStatusCode.NotFound => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.NotFound },
            _ => Ok(response)
        };
    }

    protected ActionResult HandleException(Exception ex)
    {
        Logger.Error(InternalServerErrorMessage, ex);
        // O middleware GlobalExceptionHandlerMiddleware já trata exceções não capturadas
        // Este método é mantido para compatibilidade, mas não deve ser usado frequentemente
        throw ex; // Re-throw para que o middleware trate
    }

    private ProblemDetails CreateProblemDetailsFromResponse<TData>(BaseResponse<TData> response, HttpContext context)
    {
        var problemDetails = new ProblemDetails
        {
            Type = DetermineProblemType(response.StatusCode),
            Title = DetermineProblemTitle(response.StatusCode),
            Status = response.StatusCode,
            Detail = response.Message ?? "Ocorreu um erro ao processar a requisição.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        return problemDetails;
    }

    private string DetermineProblemType(int statusCode)
    {
        return statusCode switch
        {
            (int)HttpStatusCode.BadRequest => "https://errors.hapvida.externalintegration/validation-error",
            (int)HttpStatusCode.NotFound => "https://errors.hapvida.externalintegration/not-found",
            (int)HttpStatusCode.Conflict => "https://errors.hapvida.externalintegration/conflict",
            (int)HttpStatusCode.GatewayTimeout => "https://errors.hapvida.externalintegration/timeout",
            _ => "https://errors.hapvida.externalintegration/internal-server-error"
        };
    }

    private string DetermineProblemTitle(int statusCode)
    {
        return statusCode switch
        {
            (int)HttpStatusCode.BadRequest => "Erro de validação",
            (int)HttpStatusCode.NotFound => "Recurso não encontrado",
            (int)HttpStatusCode.Conflict => "Conflito",
            (int)HttpStatusCode.GatewayTimeout => "Timeout",
            _ => "Erro interno do servidor"
        };
    }
}

