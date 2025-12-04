using Hapvida.ExternalIntegration.Domain.Entities.Response;
using Hapvida.ExternalIntegration.Domain.Resources;
using System.Collections.Generic;
using System.Linq;

namespace Hapvida.ExternalIntegration.Application.Helpers;

public class ResponseBuilder : IResponseBuilder
{
    public TResponse BuildSuccessResponse<TResponse, T>(T data, string? message = null, int statusCode = 200)
        where TResponse : BaseResponse<T>, new()
    {
        return new TResponse
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully",
            StatusCode = statusCode
        };
    }

    public TResponse BuildErrorResponse<TResponse, T>(string message, IEnumerable<string>? errors = null, int statusCode = 500)
        where TResponse : BaseResponse<T>, new()
    {
        return new TResponse
        {
            Success = false,
            Data = default!,
            Message = message ?? "An error occurred",
            Errors = errors?.ToList() ?? new List<string>(),
            StatusCode = statusCode
        };
    }

    public TResponse BuildNotFoundResponse<TResponse, T>(string? message = null)
        where TResponse : BaseResponse<T>, new()
    {
        return new TResponse
        {
            Success = false,
            Data = default!,
            Message = message ?? "Resource not found",
            StatusCode = 404
        };
    }

    public TResponse BuildValidationErrorResponse<TResponse, T>(string? message, IEnumerable<string> errors)
        where TResponse : BaseResponse<T>, new()
    {
        return new TResponse
        {
            Success = false,
            Data = default!,
            Message = message ?? "Validation failed",
            Errors = errors?.ToList() ?? new List<string>(),
            StatusCode = 400
        };
    }
}

