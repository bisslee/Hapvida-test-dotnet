using Hapvida.ExternalIntegration.Domain.Constants;
using System;
using System.Collections.Generic;

namespace Hapvida.ExternalIntegration.Domain.Entities.Response;

public abstract class BaseResponse<TData>
{
    private readonly int Code;

    protected BaseResponse(
       TData? data,
       int code = Configuration.DEFAULT_SUCCESS_CODE,
       string? message = null
       )
    {
        Data = data;
        Message = message;
        Code = code;
    }

    protected BaseResponse() => Code = Configuration.DEFAULT_SUCCESS_CODE;

    public bool Success { get; set; } = false;
    public TData? Data { get; set; }
    public bool IsSuccess => Code >= 200 && Code < 300;
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public Exception? Exception { get; set; } = null;
    public List<string> Errors { get; set; } = new List<string>();
}

public class ApiResponse<TData> : BaseResponse<TData>
{
    public ApiResponse(TData? data = default, int code = Configuration.DEFAULT_SUCCESS_CODE, string? message = null)
        : base(data, code, message)
    {
    }
}

