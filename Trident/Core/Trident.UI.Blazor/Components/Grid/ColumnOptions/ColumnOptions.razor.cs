using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace Trident.UI.Blazor.Components.Grid.ColumnOptions;

public partial class ColumnOptions<TModel> : BaseRazorComponent
{
    [Parameter]
    public IEnumerable<ColumnDefinition<TModel>> Columns { get; set; }

    [Parameter]
    public EventCallback<ColumnDefinition<TModel>> OnVisibilityToggle { get; set; }

    private async Task ToggleVisibility(ColumnDefinition<TModel> column)
    {
        await OnVisibilityToggle.InvokeAsync(column);
    }
}