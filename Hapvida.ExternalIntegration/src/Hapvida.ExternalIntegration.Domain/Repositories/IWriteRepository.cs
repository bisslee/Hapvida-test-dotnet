using System.Threading.Tasks;

namespace Hapvida.ExternalIntegration.Domain.Repositories
{
    public interface IWriteRepository<TEntity> where TEntity : class
    {
        Task<bool> Add(TEntity entity);
        Task<bool> Update(TEntity entity);
        Task<bool> Delete(TEntity entity);
        Task<bool> ExecuteSql(string sql, params object[] parameters);

    }
}

