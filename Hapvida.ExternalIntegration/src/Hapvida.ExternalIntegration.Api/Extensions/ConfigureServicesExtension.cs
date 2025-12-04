using Biss.MultiSinkLogger.ExceptionHandlers;
using Biss.MultiSinkLogger.Http;
using Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;
using Hapvida.ExternalIntegration.Domain.Constants;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.Infra.Data;
using Hapvida.ExternalIntegration.Infra.Repositories;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Reflection;

namespace Hapvida.ExternalIntegration.Api.Extensions;

public static class ConfigureServicesExtension
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        // Configurar SQLite in-memory para persistência
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite("Data Source=:memory:");
            options.EnableSensitiveDataLogging(false);
        });

        // Registrar repositórios genéricos
        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));

        // Adicionar compressão de resposta
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddTransient<HttpLoggingHandler>();
        services.AddTransient<IExceptionHandler, DefaultExceptionHandler>();

        services.AddAutoMapper();
        services.AddMediator();
        services.AddValidators();
        services.ConfigureLogging(configuration);
        services.AddApplicationServices();
        services.AddInfrastructureServices();
        services.AddHealthChecksInjection();

        // Configurar segurança
        services.ConfigureSecurity(configuration);

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[] { "en-US", "pt-BR", "es" };
            options.DefaultRequestCulture = new RequestCulture("pt-BR");
            options.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
            options.SupportedUICultures = options.SupportedCultures;
        });

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Habilitar ProblemDetails para respostas de erro automáticas
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Type = "https://errors.hapvida.externalintegration/validation-error",
                        Title = "Erro de validação",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "Um ou mais erros de validação ocorreram.",
                        Instance = context.HttpContext.Request.Path,
                        Extensions =
                        {
                            ["traceId"] = context.HttpContext.TraceIdentifier,
                            ["errors"] = context.ModelState
                                .Where(x => x.Value?.Errors.Count > 0)
                                .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                                )
                        }
                    };

                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

        services.AddEndpointsApiExplorer();
        services.ConfigureSwagger();

        // Configuração robusta do CORS
        services.ConfigureCors(configuration);

        return services;
    }

    private static void ConfigureSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar SecuritySettings
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));

        // Configurar HTTPS redirection
        var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>();
        if (securitySettings?.HttpsRedirection.EnableHttpsRedirection == true)
        {
            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = securitySettings.HttpsRedirection.HttpsPort;
            });
        }
    }

    private static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Hapvida.ExternalIntegration API",
                Version = "v1.0.0",
                Description = @"
## Hapvida.ExternalIntegration API

Esta é uma API de template para microserviços desenvolvida em .NET 8, seguindo as melhores práticas de Clean Architecture.

### Características Principais:
- **Clean Architecture**: Separação clara de responsabilidades
- **FluentValidation**: Validação robusta de dados
- **Structured Logging**: Logs estruturados com Serilog
- **Global Exception Handling**: Tratamento centralizado de exceções
- **Health Checks**: Monitoramento de saúde da aplicação
- **CORS**: Configuração robusta de Cross-Origin Resource Sharing
                ",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "dev@hapvida.com.br"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Configurar autenticação JWT (preparado para futuras implementações)
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Incluir comentários XML se existirem
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.EnableAnnotations();

            // Configurar esquemas personalizados
            c.CustomSchemaIds(type => type.Name);

            // Configurar respostas padrão
            c.MapType<DateTime>(() => new OpenApiSchema { Type = "string", Format = "date-time" });
            c.MapType<DateTime?>(() => new OpenApiSchema { Type = "string", Format = "date-time", Nullable = true });
        });
    }

    private static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            // Política para desenvolvimento
            options.AddPolicy("DevelopmentPolicy", builder =>
            {
                builder
                    .WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:3001",
                        "http://localhost:4200",
                        "http://localhost:8080",
                        "https://localhost:3000",
                        "https://localhost:3001",
                        "https://localhost:4200",
                        "https://localhost:8080"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(
                        "X-Total-Count",
                        "X-Pagination",
                        "X-Correlation-Id",
                        "X-Trace-Id"
                    )
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
            });

            // Política para produção (mais restritiva)
            options.AddPolicy("ProductionPolicy", builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

                if (allowedOrigins.Any())
                {
                    builder.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders(
                            "X-Total-Count",
                            "X-Pagination",
                            "X-Correlation-Id",
                            "X-Trace-Id"
                        );
                }
                else
                {
                    // Fallback para desenvolvimento se não configurado
                    builder
                        .WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders(
                            "X-Total-Count",
                            "X-Pagination",
                            "X-Correlation-Id",
                            "X-Trace-Id"
                        );
                }
            });

            // Política para APIs públicas (menos restritiva)
            options.AddPolicy("PublicApiPolicy", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders(
                        "X-Total-Count",
                        "X-Pagination",
                        "X-Correlation-Id",
                        "X-Trace-Id"
                    );
            });
        });
    }
}

