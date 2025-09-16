using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class GridFiltersExpandedContainer : ComponentBase
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [CascadingParameter]
    public FilterPanelContext FilterContext { get; set; }
}
