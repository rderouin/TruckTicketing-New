using System.Threading.Tasks;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class KeywordFilter : FilterComponent<string>
{
    private string _keyword;

    private async Task HandleKeywordChange()
    {
        await PropagateFilterValueChange(_keyword);
    }

    private async Task ClearSearchTerm()
    {
        _keyword = default;
        await HandleKeywordChange();
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _keyword = default;

        criteria.Keywords = default;
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        criteria.Filters ??= new();
        criteria.Keywords = _keyword;
    }
}
