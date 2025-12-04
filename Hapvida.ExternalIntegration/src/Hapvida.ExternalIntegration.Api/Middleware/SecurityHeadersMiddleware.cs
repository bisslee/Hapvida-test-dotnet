using Hapvida.ExternalIntegration.Domain.Constants;
using Microsoft.Extensions.Options;

namespace Hapvida.ExternalIntegration.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersSettings _securityHeaders;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecuritySettings> securitySettings)
    {
        _next = next;
        _securityHeaders = securitySettings.Value.Headers;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_securityHeaders.EnableSecurityHeaders)
        {
            // Previne clickjacking
            context.Response.Headers["X-Frame-Options"] = _securityHeaders.XFrameOptions;

            // Previne MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = _securityHeaders.XContentTypeOptions;

            // Proteção contra XSS
            context.Response.Headers["X-XSS-Protection"] = _securityHeaders.XssProtection;

            // Política de referrer
            context.Response.Headers["Referrer-Policy"] = _securityHeaders.ReferrerPolicy;

            // Content Security Policy (CSP)
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none';";

            // Permissions Policy
            context.Response.Headers["Permissions-Policy"] =
                "geolocation=(), microphone=(), camera=()";

            // Remove headers que podem expor informações
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");
        }

        await _next(context);
    }
}

