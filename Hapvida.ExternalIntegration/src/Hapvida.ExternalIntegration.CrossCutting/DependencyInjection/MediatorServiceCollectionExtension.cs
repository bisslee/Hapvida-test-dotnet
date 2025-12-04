using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;

public static class MediatorServiceCollectionExtension
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        var assembly = Assembly.Load("Hapvida.ExternalIntegration.Application");
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        return services;
    }
}

