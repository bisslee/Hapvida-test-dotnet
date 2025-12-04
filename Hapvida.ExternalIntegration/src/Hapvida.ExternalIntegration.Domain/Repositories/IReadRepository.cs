using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hapvida.ExternalIntegration.Domain.Repositories
{
    public interface IReadRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(Guid id);

        Task<TEntity?> GetByAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<List<TEntity>> Find(Expression<Func<TEntity, bool>> predicate);

        Task<(List<TEntity>, int)> FindWithPagination
           (
               Expression<Func<TEntity, bool>> predicate,
               int page,
               int pageSize,
               string? fieldName = null,
               string? order = null
           );

        // TODO: Implementar método ExecuteGetEntitiesSql com sintaxe correta para EF Core 9
        // Task<List<TEntity>> ExecuteGetEntitiesSql(string sql, params object[] parameters);

    }

}

