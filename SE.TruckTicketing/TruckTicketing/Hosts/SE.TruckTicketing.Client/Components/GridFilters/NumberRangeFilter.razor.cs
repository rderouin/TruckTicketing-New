using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class NumberRangeFilter : RangeFilterComponent<double>
{
    [Parameter]
    public string Format { get; set; }

    private async Task HandleChange(double? _)
    {
        await Propagate();
    }
}
