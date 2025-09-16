using System.Collections.Generic;
using System.Linq;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Utilities;

public static class CollectionFilters
{
    public static SearchResultsModel<T, SearchCriteriaModel> CollectionFilterByKeywords<T>(this ICollection<T> values, SearchCriteriaModel current)
    {
        var morePages = current.PageSize.GetValueOrDefault() * current.CurrentPage.GetValueOrDefault() < values.Count;
        return new()
        {
            Results = values
                     .Skip(current.PageSize.GetValueOrDefault() * current.CurrentPage.GetValueOrDefault())
                     .Take(current.PageSize.GetValueOrDefault()),
            Info = new()
            {
                TotalRecords = values.Count,
                NextPageCriteria = morePages
                                       ? new SearchCriteriaModel
                                       {
                                           CurrentPage = current.CurrentPage + 1,
                                           Keywords = current.Keywords,
                                           Filters = current.Filters,
                                       }
                                       : null,
                Keywords = current.Keywords,
                Filters = current.Filters,
            },
        };
    }
}
