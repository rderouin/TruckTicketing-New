using System.Threading.Tasks;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class AwaitingRemovalAckFilter : FilterComponent<bool>
{
    public const string Key = "AwaitingRemovalAckLines";

    private bool _showAwaitingRemovalAck;

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(_showAwaitingRemovalAck);
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _showAwaitingRemovalAck = false;
        criteria.Filters.Remove(Key);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_showAwaitingRemovalAck)
        {
            criteria.Filters[Key] = true;
        }
        else
        {
            criteria.Filters.Remove(Key);
        }
    }
}
