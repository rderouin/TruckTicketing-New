using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen;
using Radzen.Blazor;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentNumeric<TValue> : BaseTruckTicketingComponent
{
    private RadzenNumeric<TValue> _numeric;

    private TValue _value;

    [Parameter]
    public TValue Value
    {
        get => _value;
        set => SetPropertyValue(ref _value, value, ValueChanged);
    }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public int MaxDecimalPlaces { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<TValue> Change { get; set; }

    protected async Task OnInput(ChangeEventArgs obj)
    {
        try
        {
            var valueString = obj.Value as string;
            if (MaxDecimalPlaces < 1 || string.IsNullOrEmpty(valueString))
            {
                return;
            }

            if (valueString.EndsWith('.'))
            {
                return;
            }

            valueString = Regex.Replace(valueString, "[.]+", ".");

            // if parsing fails, change type will fail too... skip invalid inputs
            if (decimal.TryParse(valueString, out _) == false)
            {
                return;
            }

            var value = (decimal)Convert.ChangeType(valueString, typeof(decimal))!;
            var rounded = Math.Round(value, MaxDecimalPlaces, MidpointRounding.ToZero);
            var newValue = (TValue)ConvertType.ChangeType(rounded, typeof(TValue));

            var decimals = valueString.Split('.');

            if (decimals.Length > 1 && (decimals[1] == "0" || decimals[1] == ""))
            {
                await JsRuntime.InvokeAsync<string>("setNumericInputValue", _numeric.Element, valueString);
                return;
            }

            if (Equals(Value, newValue) && decimals.Length > 1 && decimals[1].Length > MaxDecimalPlaces)
            {
                await JsRuntime.InvokeAsync<string>("setNumericInputValue", _numeric.Element, valueString[..^1]);
                return;
            }

            Value = newValue;
        }
        catch
        {
            // skip invalid inputs
        }
    }
}
