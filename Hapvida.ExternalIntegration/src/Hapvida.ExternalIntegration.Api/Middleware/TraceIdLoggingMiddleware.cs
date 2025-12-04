using Serilog.Context;

namespace Hapvida.ExternalIntegration.Api.Middleware;

public class TraceIdLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Adicionar traceId a todos os logs usando Serilog Context
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await _next(context);
        }
    }
}

