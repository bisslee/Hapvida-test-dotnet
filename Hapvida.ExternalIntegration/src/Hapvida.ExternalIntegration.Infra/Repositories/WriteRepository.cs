using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Hapvida.ExternalIntegration.Infra.Repositories;

public class WriteRepository<TEntity> : IWriteRepository<TEntity> where TEntity : class
{
    private readonly AppDbContext Context;

    public WriteRepository(AppDbContext context)
    {
        Context = context;
    }

    public async Task<bool> Delete(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        Context.Set<TEntity>().Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Update(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        Context.Set<TEntity>().Update(entity);
        return await Context.SaveChangesAsync() > 0;
    }

    public async Task<bool> Add(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await Context.Set<TEntity>().AddAsync(entity);
        return await Context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExecuteSql(string sql, params object[] parameters)
    {
        if (string.IsNullOrEmpty(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

        await Context.Database.ExecuteSqlRawAsync(sql, parameters);
        return true;
    }
}

