using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Hapvida.ExternalIntegration.Api.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Meter Meter = new("Hapvida.ExternalIntegration.Api");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "http_requests_total",
        "requests",
        "Total number of HTTP requests");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "http_request_duration_seconds",
        "seconds",
        "HTTP request duration in seconds");

    public MetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var route = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;
            var statusCode = context.Response.StatusCode;

            // Incrementar contador de requisições
            RequestCounter.Add(1, new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("route", route),
                new KeyValuePair<string, object?>("status_code", statusCode));

            // Registrar duração da requisição
            RequestDuration.Record(duration, new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("route", route),
                new KeyValuePair<string, object?>("status_code", statusCode));
        }
    }
}

