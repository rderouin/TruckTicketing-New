using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen.Blazor;

namespace SE.TruckTicketing.Client.Components.RadzenExtensions;

public partial class TridentMaskedTextBox : BaseTruckTicketingComponent
{
    private string _mask = "";

    private Guid _maskedInputId = Guid.NewGuid();

    private string _value;

    protected RadzenTextBox TextBox;

    [Parameter]
    public string Value
    {
        get => _value;
        set => SetPropertyValue(ref _value, value, new(this, UpdateInputMaskValue));
    }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<string> Change { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string Mask { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetMask(_mask);
        }
    }

    protected override void OnParametersSet()
    {
        SetPropertyValue(ref _mask, Mask, new(this, SetMask));
    }

    private async Task SetMask(string mask)
    {
        await JsRuntime.InvokeAsync<dynamic>("setInputMask", _maskedInputId, TextBox.Element, mask ?? string.Empty);
    }

    private async Task UpdateInputMaskValue()
    {
        await JsRuntime.InvokeAsync<dynamic>("updateInputMask", _maskedInputId);
    }

    public override void Dispose()
    {
        JsRuntime.InvokeAsync<dynamic>("disposeInputMask", _maskedInputId);
    }
}
