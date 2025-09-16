using System.Linq;

using Radzen;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components;

public static class LoadDataArgsExtensions
{
    public static SearchCriteriaModel ToSearchCriteriaModel(this LoadDataArgs args)
    {
        var criteria = new SearchCriteriaModel();

        if (args.Filters != null)
        {
            foreach (var filter in args.Filters)
            {
                criteria.Filters[filter.Property] = filter.FilterValue;
            }
        }

        if (args.Sorts is not null)
        {
            if (args.Sorts.Count() > 1)
            {
                foreach (var sort in args.Sorts)
                {
                    criteria.MultiOrderBy[sort.Property] = sort.SortOrder == SortOrder.Ascending
                                                               ? Trident.Contracts.Enums.SortOrder.Asc
                                                               : Trident.Contracts.Enums.SortOrder.Desc;
                }
            }
            else
            {
                var firstSort = args.Sorts.FirstOrDefault();
                criteria.OrderBy = firstSort?.Property ?? string.Empty;
                criteria.SortOrder = firstSort is { SortOrder: SortOrder.Descending }
                                         ? Trident.Contracts.Enums.SortOrder.Desc
                                         : Trident.Contracts.Enums.SortOrder.Asc;
            }
        }
        else if (!string.IsNullOrWhiteSpace(args.OrderBy))
        {
            var sorts = args.OrderBy.Split();
            criteria.OrderBy = sorts[0].Trim('@');
            criteria.SortOrder = sorts[1] == "asc" ? Trident.Contracts.Enums.SortOrder.Asc : Trident.Contracts.Enums.SortOrder.Desc;
        }

        else if (!string.IsNullOrEmpty(args.OrderBy))
        {
            criteria.OrderBy = args.OrderBy.Split(" ").First().Replace("@", "");
        }

        criteria.Keywords = args.Filter;

        criteria.PageSize = args.Top ?? 10;
        if (criteria.PageSize == 0)
        {
            criteria.PageSize = 10;
        }
        criteria.CurrentPage = (args.Skip ?? 0) / criteria.PageSize;

        return criteria;
    }
}
