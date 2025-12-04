using Hapvida.ExternalIntegration.Api.Extensions;
using Hapvida.ExternalIntegration.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Hapvida.ExternalIntegration.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configura os serviços (inclui configuração do Serilog)
            builder.Services.ConfigureServices(builder.Configuration);

            // Configura o Serilog no host (deve ser após ConfigureServices)
            builder.Host.UseSerilog();

            var app = builder.Build();

            // Garantir que o banco de dados em memória seja criado
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }

            // Configura Middlewares
            app.ConfigureMiddlewares();

            // Mapear endpoint de health check
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
