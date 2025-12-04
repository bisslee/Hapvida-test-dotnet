using Biss.MultiSinkLogger.Extensions;
using Hapvida.ExternalIntegration.Api.Middleware;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Hapvida.ExternalIntegration.Api.Extensions;

public static class ConfigureMiddlewaresExtension
{
    public static WebApplication ConfigureMiddlewares(this WebApplication app)
    {
        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()?.Value;
        if (localizationOptions != null)
        {
            app.UseRequestLocalization(localizationOptions);
        }

        // Adicionar compressão de resposta
        app.UseResponseCompression();

        app.UseStructuredLogging();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        app.UseExceptionLogging();
        app.UseCustomLogging();

        // Middlewares de segurança
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Configurar CORS baseado no ambiente
        var environment = app.Environment.EnvironmentName;
        var corsPolicy = environment switch
        {
            "Development" => "DevelopmentPolicy",
            "Production" => "ProductionPolicy",
            _ => "PublicApiPolicy"
        };

        app.UseCors(corsPolicy);

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}

