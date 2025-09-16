using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Trident.Contracts.Api;
using Trident.Data;
using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Search;

namespace Trident.EFCore
{
    public abstract class CosmosEFCoreSearchRepositoryBase<TEntity> : EFCoreSearchRepositoryBase<TEntity>
        where TEntity : Entity
    {
        // ReSharper disable once StaticMemberInGenericType - for each closed type, there is a different partition key, intended to be this way
        private static readonly string WellKnownPartitionKey;

        private readonly bool _disablePartitionKeyInference;

        static CosmosEFCoreSearchRepositoryBase()
        {
            // init the well-known partition key once
            var containerAttribute = typeof(TEntity).GetCustomAttribute<ContainerAttribute>();
            if (containerAttribute != null)
            {
                WellKnownPartitionKey = containerAttribute.PartitionKeyType == PartitionKeyType.WellKnown
                                            ? containerAttribute.PartitionKeyValue
                                            : null;
            }
        }

        protected CosmosEFCoreSearchRepositoryBase(ISearchResultsBuilder resultsBuilder,
                                                   ISearchQueryBuilder queryBuilder,
                                                   IAbstractContextFactory abstractContextFactory,
                                                   IQueryableHelper queryableHelper)
            : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
        {
            if (Options.TryGetValue(DisablePartitionKeyInferenceOption, out var disablePartitionKeyInferenceObject) &&
                disablePartitionKeyInferenceObject is bool disablePartitionKeyInference)
            {
                _disablePartitionKeyInference = disablePartitionKeyInference;
            }
        }

        public override async Task<TEntity> GetById(object id, bool detach = false)
        {
            return await GetBaseQuery(detach, null).Where(e => e.Id == id).FirstOrDefaultAsync();
        }

        public override async Task<TEntity> GetById(CompositeKey<object> key, bool detach = false)
        {
            var (id, partitionKey) = (key.Id, key.PartitionKey);
            return await GetBaseQuery(detach, partitionKey).Where(e => e.Id == id).FirstOrDefaultAsync();
        }

        public override async Task<IEnumerable<TEntity>> GetByIds<TEntityId>(IEnumerable<TEntityId> ids, bool detach = false)
        {
            return await GetBaseQuery(detach, null).Where(x => ids.Contains((TEntityId)x.Id)).ToListAsync();
        }

        public override async Task<IEnumerable<TEntity>> GetByIds<TEntityId>(IEnumerable<CompositeKey<TEntityId>> keys, bool detach = false)
        {
            var entities = new List<TEntity>();

            // group by a partition key
            foreach (var batch in keys.GroupBy(k => k.PartitionKey))
            {
                // single batch
                var customPartitionKey = batch.Key;
                var batchedIds = batch.Select(k => (object)k.Id).ToList();

                // partition-based query
                var entityBatch = await GetBaseQuery(detach, customPartitionKey).Where(x => batchedIds.Contains(x.Id)).ToListAsync();

                // add the batch to the list
                entities.AddRange(entityBatch);
            }

            return entities;
        }

        public override async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter = null,
                                                             Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                             IEnumerable<string> includeProperties = null,
                                                             bool noTracking = false)
        {
            return await Get(filter, null, orderBy, includeProperties, noTracking);
        }

        public override async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter = null,
                                                             string partitionKey = null,
                                                             Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                             IEnumerable<string> includeProperties = null,
                                                             bool noTracking = false)
        {
            var query = GetBaseQuery(noTracking, partitionKey);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync();
        }

        public override async Task<bool> Exists(Expression<Func<TEntity, bool>> filter)
        {
            var query = GetBaseQuery(true, null);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var entity = await query.FirstOrDefaultAsync();

            return entity != default;
        }

        public override async Task<List<TEntity>> GetNativeAsync<T, TId>(Expression<Func<T, bool>> filter)
        {
            var client = Context.GetDbClient<T>();
            var nativeList = await client.QueryAsync<T, TId>(filter);
            var keys = nativeList.Select(e => e.Key).ToList();
            var list = await GetByIds(keys);
            return list.ToList();
        }

        public override async Task<HttpStatusCode> SaveNativeAsync<T, TId>(IEnumerable<T> entities, string partitionKey, CancellationToken cancellationToken = default)
        {
            var client = Context.GetDbClient<T>();
            var success = await client.SaveEntities<T, TId>(entities, partitionKey, cancellationToken);
            return success;
        }

        public override async Task<SearchResults<TEntity, SearchCriteria>> Search(SearchCriteria searchCriteria, IEnumerable<string> includedProperties = null)
        {
            var query = BuildQuery(searchCriteria, includedProperties);

            // apply a partition key
            if (WellKnownPartitionKey != null)
            {
                query = WithPartitionKey(query, WellKnownPartitionKey);
            }
            else if (searchCriteria.Filters.TryGetValue(nameof(DocumentDbEntityBase<Guid>.DocumentType), out var documentTypeValue) &&
                     documentTypeValue is string documentType &&
                     documentType.Length > 0)
            {
                query = WithPartitionKey(query, documentType);
            }

            // Get total Records before returning results
            var totalRecords = await query.CountAsync();

            //apply paging
            query = ApplyPaging(query, searchCriteria);

            var results = await query.ToListAsync();

            return SearchResultContent(results, searchCriteria, totalRecords);
        }

        private IQueryable<TEntity> GetBaseQuery(bool detach, string customPartitionKey)
        {
            // get the base query
            var query = Context.Query<TEntity>();

            // apply a partition key
            var partitionKey = customPartitionKey ?? WellKnownPartitionKey;
            if (partitionKey != null)
            {
                query = WithPartitionKey(query, partitionKey);
            }

            // apply no-tracking
            if (detach)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        private IQueryable<TEntity> WithPartitionKey(IQueryable<TEntity> query, string partitionKey)
        {
            return _disablePartitionKeyInference
                       ? query
                       : query.WithPartitionKey(partitionKey);
        }
    }
}
