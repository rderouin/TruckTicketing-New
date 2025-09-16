using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class DateRangeFilter : RangeFilterComponent<DateTime>
{
    [Parameter]
    public string Format { get; set; } = "d";

    private async Task HandleDateChange(DateTime? _)
    {
        await Propagate();
    }
}
