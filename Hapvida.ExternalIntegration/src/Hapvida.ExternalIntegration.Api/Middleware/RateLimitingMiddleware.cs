using Hapvida.ExternalIntegration.Domain.Constants;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Hapvida.ExternalIntegration.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingSettings _rateLimitingSettings;
    private readonly RateLimiter? _rateLimiter;

    public RateLimitingMiddleware(RequestDelegate next, IOptions<SecuritySettings> securitySettings)
    {
        _next = next;
        _rateLimitingSettings = securitySettings.Value.RateLimiting;

        if (_rateLimitingSettings.EnableRateLimiting)
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = _rateLimitingSettings.PermitLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.Parse(_rateLimitingSettings.Window),
                TokensPerPeriod = _rateLimitingSettings.PermitLimit,
                AutoReplenishment = true
            };

            _rateLimiter = new TokenBucketRateLimiter(options);
        }
        else
        {
            _rateLimiter = null;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_rateLimitingSettings.EnableRateLimiting && _rateLimiter != null)
        {
            using var lease = await _rateLimiter.AcquireAsync();

            if (lease.IsAcquired)
            {
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter.TotalSeconds
                        : 60
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
        else
        {
            await _next(context);
        }
    }
}

