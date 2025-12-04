using Hapvida.ExternalIntegration.Domain.Entities.Response;
using System.Collections.Generic;

namespace Hapvida.ExternalIntegration.Application.Helpers;

public interface IResponseBuilder
{
    TResponse BuildSuccessResponse<TResponse, T>(T data, string? message = null, int statusCode = 200)
        where TResponse : BaseResponse<T>, new();
    TResponse BuildErrorResponse<TResponse, T>(string message, IEnumerable<string>? errors = null, int statusCode = 500)
        where TResponse : BaseResponse<T>, new();
    TResponse BuildNotFoundResponse<TResponse, T>(string? message = null)
        where TResponse : BaseResponse<T>, new();
    TResponse BuildValidationErrorResponse<TResponse, T>(string? message, IEnumerable<string> errors)
        where TResponse : BaseResponse<T>, new();
}

