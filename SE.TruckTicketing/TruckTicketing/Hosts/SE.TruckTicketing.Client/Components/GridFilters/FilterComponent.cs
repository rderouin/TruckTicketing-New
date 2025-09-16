using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public abstract class FilterComponent : BaseRazorComponent
{
    [CascadingParameter]
    public FilterPanelContext FilterContext { get; set; }

    [Parameter]
    public string Id { get; set; }

    [Parameter]
    public string Label { get; set; }

    [Parameter]
    public string FilterPath { get; set; }

    [Parameter]
    public string Placeholder { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        FilterContext.RegisterFilterComponent(this);
    }

    public abstract void Reset(SearchCriteriaModel criteria);

    public abstract void ApplyFilter(SearchCriteriaModel criteria);
}

public abstract class FilterComponent<TValue> : FilterComponent
{
    [Parameter]
    public EventCallback<FilterComponentChangeArgs<TValue>> OnChange { get; set; }

    protected async Task PropagateFilterValueChange(TValue value)
    {
        ApplyFilter(FilterContext.SearchCriteriaModel);
        FilterContext.SearchCriteriaModel.CurrentPage = 0;

        if (OnChange.HasDelegate)
        {
            await OnChange.InvokeAsync(new()
            {
                Value = value,
                Ref = this,
            });
        }

        FilterContext.RaiseChangeEvent();
    }
}
