using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SE.TruckTicketing.Client.Components.RadzenExtensions;

public class TridentElementAttributeMarker : BaseTruckTicketingComponent
{
    private List<string> _markedItems = new();

    private string _markers;

    [Parameter]
    public string ContainerId { get; set; }

    [Parameter]
    public string ItemMarkerAttribute { get; set; }

    [Parameter]
    public string ItemMarkedAttribute { get; set; }

    [Parameter]
    public string ItemSelector { get; set; }

    [Parameter]
    public string ParentSelector { get; set; }

    [Inject]
    public IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public IEnumerable<string> MarkedItems { get; set; } = Array.Empty<string>();

    protected override async Task OnParametersSetAsync()
    {
        var markers = string.Join("", MarkedItems);
        if (_markers == markers)
        {
            return;
        }

        var itemsToMark = new Dictionary<string, Dictionary<string, string>>();

        foreach (var item in _markedItems)
        {
            itemsToMark[SelectorFor(item)] = new() { [ItemMarkedAttribute] = "false" };
        }

        foreach (var item in MarkedItems)
        {
            itemsToMark[SelectorFor(item)] = new() { [ItemMarkedAttribute] = "true" };
        }

        await ApplyMarkerAttributes(itemsToMark);

        _markers = markers;
        _markedItems = MarkedItems.ToList();
    }

    private string SelectorFor(string item)
    {
        return $"{ItemSelector}[{ItemMarkerAttribute}='{item}']";
    }

    private async Task ApplyMarkerAttributes(Dictionary<string, Dictionary<string, string>> itemsToMark)
    {
        await JsRuntime.InvokeVoidAsync("setMarkerAttributes", ContainerId, itemsToMark, ParentSelector);
    }
}
