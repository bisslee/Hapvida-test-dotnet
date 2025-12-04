using Hapvida.ExternalIntegration.Application.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;

public static class AutoMapperServiceCollectionExtension
{
    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());

        return services;
    }
}

