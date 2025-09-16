using System;
using System.Linq;
using System.Threading.Tasks;

using Trident.Contracts;
using Trident.Domain;
using Trident.Search;

namespace SE.TridentContrib.Extensions.Managers;

public static class ManagerExtensions
{
    public static async Task RunBatchedAction<TEntity>(this IManager<Guid, TEntity> manager, SearchCriteria criteria, Func<TEntity[], Task> action, int pageSize = 100)
        where TEntity : EntityBase<Guid>
    {
        SearchResults<TEntity, SearchCriteria> search;

        criteria.PageSize = pageSize;
        int? pageCount = null;

        do
        {
            search = await manager.Search(criteria);
            pageCount ??= search.Info.PageCount;
            var items = search?.Results.ToArray();
            if (items is null || items.Length == 0)
            {
                return;
            }

            await action(items);
            criteria.CurrentPage += 1;
        } while (search.Info.CurrentPage <= pageCount);
    }
}
