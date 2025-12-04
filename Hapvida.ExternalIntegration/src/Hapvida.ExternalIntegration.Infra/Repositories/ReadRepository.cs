using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace Hapvida.ExternalIntegration.Infra.Repositories;

public class ReadRepository<TEntity> : IReadRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext Context;
    private readonly ILogger<ReadRepository<TEntity>> _logger;

    public ReadRepository(
        AppDbContext context,
        ILogger<ReadRepository<TEntity>> logger)
    {
        Context = context;
        _logger = logger;
    }

    public async Task<List<TEntity>> Find(Expression<Func<TEntity, bool>> predicate)
    {
        _logger.LogDebug("Executing Find query with predicate");
        return await Context.Set<TEntity>()
            .AsNoTracking() // Otimização: não tracking para consultas
            .Where(predicate)
            .ToListAsync();
    }

    public async Task<(List<TEntity>, int)> FindWithPagination
    (
        Expression<Func<TEntity, bool>> predicate,
        int page,
        int pageSize,
        string? fieldName = null,
        string? order = null
    )
    {
        _logger.LogDebug("Executing FindWithPagination. Page: {Page}, PageSize: {PageSize}, FieldName: {FieldName}, Order: {Order}",
            page, pageSize, fieldName, order);

        // Otimização: Executar count e data em paralelo
        var query = Context.Set<TEntity>()
            .AsNoTracking() // Otimização: não tracking para consultas
            .Where(predicate);

        // Executar count e data em paralelo para melhor performance
        var countTask = query.CountAsync();

        if (!string.IsNullOrEmpty(fieldName))
        {
            var ordering = fieldName +
                (string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase)
                    ? " descending" : "");
            query = query.OrderBy(ordering);
        }

        var dataTask = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Aguardar ambas as tarefas em paralelo
        await Task.WhenAll(countTask, dataTask);

        var total = await countTask;
        var data = await dataTask;

        _logger.LogDebug("FindWithPagination completed. Total: {Total}, Returned: {Returned}", total, data.Count);

        return (data, total);
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Executing GetByIdAsync for ID: {Id}", id);
        return await Context.Set<TEntity>()
            .AsNoTracking() // Otimização: não tracking para consultas
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }

    public async Task<TEntity?> GetByAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing GetByAsync query with predicate");
        return await Context.Set<TEntity>()
            .AsNoTracking() // Otimização: não tracking para consultas
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }
}

