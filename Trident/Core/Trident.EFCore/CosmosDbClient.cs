using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

using Newtonsoft.Json.Linq;

using Trident.Data;
using Trident.Domain;

namespace Trident.EFCore
{
    public class CosmosDbClient<T> : IDbClient<T>
    {
        private readonly Container _container;

        private readonly DataSourceInfo _dbInfo;

        public CosmosDbClient(Container container, DataSourceInfo dbInfo)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _dbInfo = dbInfo ?? throw new ArgumentNullException(nameof(dbInfo));
        }

        public async Task<IEnumerable<JObject>> ExecuteQueryAsync(string command, IDictionary<string, object> parameters = null)
        {
            var queryDefinition = new QueryDefinition(command);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    queryDefinition.WithParameter(param.Key, param.Value);
                }
            }

            var resultSet = _container.GetItemQueryIterator<JObject>(queryDefinition);
            var results = await resultSet.ReadNextAsync();
            return results;
        }

        public async Task<List<TEntity>> QueryAsync<TEntity, TId>(Expression<Func<TEntity, bool>> filter) where TEntity : DocumentDbEntityBase<TId>
        {
            var list = new List<TEntity>();

            // get the container queryable
            var queryable = _container.GetItemLinqQueryable<TEntity>();

            // execute the query
            var iterator = queryable.Where(e => e.EntityType == _dbInfo.DiscriminatorValue)
                                    .Where(filter)
                                    .ToFeedIterator();

            // fetch data
            while (iterator.HasMoreResults)
            {
                var batch = await iterator.ReadNextAsync();
                list.AddRange(batch);
            }

            return list;
        }

        public async Task<HttpStatusCode> SaveEntities<TEntity, TId>(IEnumerable<TEntity> entities, string partitionKey, CancellationToken cancellationToken = default) where TEntity : DocumentDbEntityBase<TId>
        {
            // a transactional batch for the set of entities
            var transactionalBatch = _container.CreateTransactionalBatch(new PartitionKey(partitionKey));

            // all entities should be saved (upsert)
            foreach (var entity in entities)
            {
                // validate the correctness of the set
                if (entity.DocumentType != partitionKey)
                {
                    throw new InvalidOperationException($"The partition keys of a transaction ({partitionKey}) and the entity ({entity.DocumentType}) do not match.");
                }

                // save op
                transactionalBatch.UpsertItem(entity);
            }

            // execute the transaction
            using var response = await transactionalBatch.ExecuteAsync(cancellationToken);

            // success of the batch execution
            return response.StatusCode;
        }
    }
}
