using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Utilities;

namespace SE.TruckTicketing.Client.Components;

public partial class CustomTimePicker
{
    private DateTimeOffset? _time;

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public string Class { get; set; }

    [Parameter]
    public DateTimeOffset? Time
    {
        get => _time;
        set => SetPropertyValue(ref _time, value, TimeChanged);
    }

    [Parameter]
    public EventCallback<DateTimeOffset?> TimeChanged { get; set; }

    [Parameter]
    public string DateFormat { get; set; } = "hh:mm tt";

    [Parameter]
    public bool Disabled { get; set; }

    private string GetValue()
    {
        // format the time only
        var formattedValue = Time?.ToString(DateFormat) ?? string.Empty;
        return formattedValue;
    }

    private async Task SetValue(ChangeEventArgs e)
    {
        DateTimeOffset? newFullTime = null;
        try
        {
            var value = e?.Value?.ToString();

            // blank field
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            // parse the time
            var parsedTime = TimeParser.Parse(value);

            // failed parsing the time
            if (parsedTime == null)
            {
                return;
            }

            // determine the starting DateTimeOffset
            var currentFullTime = Time ?? DateTimeOffset.Now;

            // calculate the new time
            newFullTime = currentFullTime - currentFullTime.TimeOfDay + parsedTime;
        }
        finally
        {
            await RunMagicalForceUpdate(() => Time, t => Time = t, newFullTime, default(DateTimeOffset));
        }
    }
}
