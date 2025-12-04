using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hapvida.ExternalIntegration.Domain.Entities.Response;

public class ErrorResponse
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("validationErrors")]
    public List<ValidationError>? ValidationErrors { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    public ErrorResponse()
    {
    }

    public ErrorResponse(string errorCode, string message, string? details = null)
    {
        ErrorCode = errorCode;
        Message = message;
        Details = details;
    }

    public ErrorResponse(string errorCode, string message, List<ValidationError> validationErrors)
    {
        ErrorCode = errorCode;
        Message = message;
        ValidationErrors = validationErrors;
    }
}

public class ValidationError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    public ValidationError()
    {
    }

    public ValidationError(string field, string message, object? value = null)
    {
        Field = field;
        Message = message;
        Value = value;
    }
}

