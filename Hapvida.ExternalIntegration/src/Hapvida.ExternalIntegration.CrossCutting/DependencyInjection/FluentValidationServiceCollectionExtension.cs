using FluentValidation;
using Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;

public static class FluentValidationServiceCollectionExtension
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        // Registra todos os validadores da assembly do application
        var assembly = Assembly.Load("Hapvida.ExternalIntegration.Application");
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        return services;
    }
}

