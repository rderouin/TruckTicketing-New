using System.Threading.Tasks;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class PreviewSalesLinesFilter : FilterComponent<bool>
{
    public const string Key = "PreviewLines";

    private bool _showPreviewSalesLines;

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(_showPreviewSalesLines);
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _showPreviewSalesLines = false;
        criteria.Filters.Remove(Key);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_showPreviewSalesLines)
        {
            criteria.Filters[Key] = true;
        }
        else
        {
            criteria.Filters.Remove(Key);
        }
    }
}
