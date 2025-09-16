using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Trident.Domain;

namespace Trident.Data
{
    public interface IDbClient<T>
    {
        Task<IEnumerable<JObject>> ExecuteQueryAsync(string command, IDictionary<string, object> parameters = null);

        Task<List<TEntity>> QueryAsync<TEntity, TId>(Expression<Func<TEntity, bool>> filter) where TEntity : DocumentDbEntityBase<TId>;

        Task<HttpStatusCode> SaveEntities<TEntity, TId>(IEnumerable<TEntity> entities, string partitionKey, CancellationToken cancellationToken = default) where TEntity : DocumentDbEntityBase<TId>;
    }
}
