using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Hapvida.ExternalIntegration.CrossCutting.Health;

public class ApiHealthCheck : IHealthCheck
{
    private readonly ILogger<ApiHealthCheck> _logger;
    private static readonly Stopwatch _stopwatch = new();

    public ApiHealthCheck(ILogger<ApiHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing API Health Check");

        try
        {
            _stopwatch.Restart();

            // Simular algumas verificações básicas
            await Task.Delay(10, cancellationToken); // Simular processamento

            _stopwatch.Stop();

            var healthData = new Dictionary<string, object>
            {
                { "ResponseTime", $"{_stopwatch.ElapsedMilliseconds}ms" },
                { "Timestamp", DateTime.UtcNow },
                { "Version", GetAssemblyVersion() },
                { "Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown" },
                { "MachineName", Environment.MachineName },
                { "ProcessId", Environment.ProcessId },
                { "MemoryUsage", GC.GetTotalMemory(false) }
            };

            _logger.LogInformation("API Health Check completed successfully in {ResponseTime}ms", _stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("API is healthy", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Health Check failed");
            return HealthCheckResult.Unhealthy("API health check failed", ex);
        }
    }

    private static string GetAssemblyVersion()
    {
        try
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

