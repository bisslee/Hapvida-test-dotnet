using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Services;
using Hapvida.ExternalIntegration.Infra.ExternalApis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application Services
        services.AddScoped<ICepService, CepService>();
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IResponseBuilder, ResponseBuilder>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // HTTP Clients - CEP Providers com resiliência
        services.AddHttpClient<BrasilApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://brasilapi.com.br");
        })
        .AddCepProviderResilience("BrasilApiClient");
        
        services.AddScoped<ICepProvider, BrasilApiClient>(sp => sp.GetRequiredService<BrasilApiClient>());

        services.AddHttpClient<ViaCepClient>(client =>
        {
            client.BaseAddress = new Uri("https://viacep.com.br");
        })
        .AddCepProviderResilience("ViaCepClient");
        
        services.AddScoped<ICepProvider, ViaCepClient>(sp => sp.GetRequiredService<ViaCepClient>());

        // HTTP Client - Weather Provider com resiliência
        services.AddHttpClient<Hapvida.ExternalIntegration.Infra.ExternalApis.OpenMeteoClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com");
        })
        .AddResiliencePolicies("OpenMeteoClient");
        
        services.AddScoped<IWeatherProvider, Hapvida.ExternalIntegration.Infra.ExternalApis.OpenMeteoClient>(
            sp => sp.GetRequiredService<Hapvida.ExternalIntegration.Infra.ExternalApis.OpenMeteoClient>());

        // Memory Cache
        services.AddMemoryCache();

        return services;
    }
}

