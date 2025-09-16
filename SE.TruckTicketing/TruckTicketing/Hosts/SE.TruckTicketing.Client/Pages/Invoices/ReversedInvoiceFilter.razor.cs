using System.Threading.Tasks;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Pages.Invoices;

public partial class ReversedInvoiceFilter : FilterComponent<bool>
{
    private bool _showReversedInvoices;

    public override void Reset(SearchCriteriaModel criteria)
    {
        _showReversedInvoices = false;

        criteria.Filters[nameof(Invoice.IsReversed)] = false;
        criteria.Filters[nameof(Invoice.IsReversal)] = false;
    }

    private async Task HandleChange(bool value)
    {
        await PropagateFilterValueChange(value);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (!_showReversedInvoices)
        {
            criteria.Filters[nameof(Invoice.IsReversed)] = false;
            criteria.Filters[nameof(Invoice.IsReversal)] = false;
        }
        else
        {
            criteria.Filters.Remove(nameof(Invoice.IsReversed));
            criteria.Filters.Remove(nameof(Invoice.IsReversal));
        }
    }
}
