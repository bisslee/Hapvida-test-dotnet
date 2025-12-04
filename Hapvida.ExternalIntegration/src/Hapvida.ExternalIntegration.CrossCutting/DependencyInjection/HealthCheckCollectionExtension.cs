using Hapvida.ExternalIntegration.CrossCutting.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;

public static class HealthCheckCollectionExtension
{
    public static IServiceCollection AddHealthChecksInjection(this IServiceCollection services)
    {
        services.AddHealthChecks()
            // Health check da API
            .AddCheck<ApiHealthCheck>(
                name: "api_health",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "api", "core" },
                timeout: TimeSpan.FromSeconds(10));

        return services;
    }
}

