using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Hapvida.ExternalIntegration.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private const string ProblemDetailsContentType = "application/problem+json";

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        _logger.LogError(exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, TraceId: {TraceId}, Path: {Path}, Method: {Method}",
            correlationId, traceId, context.Request.Path, context.Request.Method);

        var problemDetails = CreateProblemDetails(exception, context, traceId);
        var statusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = ProblemDetailsContentType;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, jsonOptions);

        if (!context.Response.HasStarted && context.Response.Body.CanWrite)
        {
            try
            {
                await context.Response.WriteAsync(jsonResponse);
            }
            catch (ObjectDisposedException)
            {
                // Stream já foi fechado, não faz nada
            }
        }
    }

    private ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, string traceId)
    {
        var problemDetails = exception switch
        {
            FluentValidation.ValidationException validationEx => CreateValidationProblemDetails(validationEx, context, traceId),
            ArgumentException argEx => CreateArgumentProblemDetails(argEx, context, traceId),
            UnauthorizedAccessException => CreateUnauthorizedProblemDetails(context, traceId),
            TimeoutException => CreateTimeoutProblemDetails(context, traceId),
            HttpRequestException httpEx when httpEx.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) 
                => CreateTimeoutProblemDetails(context, traceId),
            _ => CreateInternalServerErrorProblemDetails(exception, context, traceId)
        };

        return problemDetails;
    }

    private ProblemDetails CreateValidationProblemDetails(
        FluentValidation.ValidationException exception,
        HttpContext context,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://errors.hapvida.externalintegration/validation-error",
            Title = "Erro de validação",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "Um ou mais erros de validação ocorreram.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId,
                ["errors"] = exception.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage,
                    attemptedValue = e.AttemptedValue?.ToString()
                }).ToArray()
            }
        };

        return problemDetails;
    }

    private ProblemDetails CreateArgumentProblemDetails(
        ArgumentException exception,
        HttpContext context,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://errors.hapvida.externalintegration/invalid-argument",
            Title = "Argumento inválido",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        return problemDetails;
    }

    private ProblemDetails CreateUnauthorizedProblemDetails(
        HttpContext context,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://errors.hapvida.externalintegration/unauthorized",
            Title = "Não autorizado",
            Status = (int)HttpStatusCode.Unauthorized,
            Detail = "Acesso negado. Você não está autorizado a realizar esta ação.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        return problemDetails;
    }

    private ProblemDetails CreateTimeoutProblemDetails(
        HttpContext context,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://errors.hapvida.externalintegration/timeout",
            Title = "Timeout",
            Status = (int)HttpStatusCode.GatewayTimeout,
            Detail = "A requisição ao provedor externo excedeu o tempo limite.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        return problemDetails;
    }

    private ProblemDetails CreateInternalServerErrorProblemDetails(
        Exception exception,
        HttpContext context,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://errors.hapvida.externalintegration/internal-server-error",
            Title = "Erro interno do servidor",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = _environment.IsDevelopment()
                ? exception.Message
                : "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
        }

        return problemDetails;
    }
}
