using Biss.MultiSinkLogger;
using Hapvida.ExternalIntegration.Api.Middleware;
using Microsoft.AspNetCore.HttpLogging;
using Serilog;
using Serilog.Context;

namespace Hapvida.ExternalIntegration.Api.Extensions;

public static class LoggingExtension
{
    public static IServiceCollection ConfigureLogging(this IServiceCollection services, IConfiguration configuration)
    {
        // Configura o Biss.MultiSinkLogger
        LoggingManager.InitializeLogger(configuration);

        // Configura o Serilog no host com output JSON estruturado
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Hapvida.ExternalIntegration.Api")
            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
            .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter(renderMessage: true))
            .WriteTo.File(
                new Serilog.Formatting.Json.JsonFormatter(renderMessage: true),
                path: "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Logger = logger;

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(logger, dispose: true);
        });

        // Configura HTTP logging para capturar requisições e respostas
        services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
            logging.RequestHeaders.Add("Authorization");
            logging.RequestHeaders.Add("X-Correlation-ID");
            logging.ResponseHeaders.Add("X-Correlation-ID");
            logging.MediaTypeOptions.AddText("application/json");
            logging.RequestBodyLogLimit = 4096;
            logging.ResponseBodyLogLimit = 4096;
        });

        return services;
    }

    public static IApplicationBuilder UseStructuredLogging(this IApplicationBuilder app)
    {
        // Adiciona middleware para capturar correlation ID e traceId
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Response.Headers["X-Correlation-ID"] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
            {
                await next();
            }
        });

        // Adiciona middleware para garantir traceId em todos os logs
        app.UseMiddleware<TraceIdLoggingMiddleware>();

        // Adiciona middleware para métricas
        app.UseMiddleware<MetricsMiddleware>();

        return app;
    }
}

