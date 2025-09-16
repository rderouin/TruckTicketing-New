using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentApiDataList<TModel, TValue> : BaseTruckTicketingComponent
    where TModel : class, IGuidModelBase
{
    protected RadzenDataList<TModel> DataList;

    [Parameter]
    public bool AllowPaging { get; set; } = true;

    [Parameter]
    public string Id { get; set; }

    [Parameter]
    public bool WrapItems { get; set; }

    [Parameter]
    public IEnumerable<TModel> Data { get; set; }

    [Parameter]
    public bool ShowPagingSummary { get; set; } = true;

    [Parameter]
    public string Class { get; set; }

    [Parameter]
    public string Style { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public HorizontalAlign PagerHorizontalAlign { get; set; } = HorizontalAlign.Left;

    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public RenderFragment<TModel> Template { get; set; }

    [Parameter]
    public string TextProperty { get; set; }

    [Parameter]
    public EventCallback<bool> LoadingCompleted { get; set; }
}
