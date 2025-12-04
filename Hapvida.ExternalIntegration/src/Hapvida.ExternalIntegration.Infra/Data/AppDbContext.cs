using Hapvida.ExternalIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;

namespace Hapvida.ExternalIntegration.Infra.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ZipCodeLookup> ZipCodeLookups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configurações de performance para ZipCodeLookup
        modelBuilder.Entity<ZipCodeLookup>(entity =>
        {
            entity.HasIndex(e => e.ZipCode).IsUnique(); // Não permitir entradas repetidas
            entity.HasIndex(e => e.City); // Índice para busca por cidade
            entity.HasIndex(e => e.State); // Índice para busca por estado
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Otimizações de performance
        optionsBuilder
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // Desabilita tracking por padrão
            .EnableSensitiveDataLogging(false) // Desabilita logs sensíveis em produção
            .EnableDetailedErrors(false); // Desabilita erros detalhados em produção
    }
}

