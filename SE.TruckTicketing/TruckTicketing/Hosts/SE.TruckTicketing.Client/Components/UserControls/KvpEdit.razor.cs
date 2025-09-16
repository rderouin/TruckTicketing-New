using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class KvpEdit
{
    [Parameter]
    public Dictionary<string, string> Data { get; set; }

    protected override void OnParametersSet()
    {
    }

    private void AddRow()
    {
        if (Data.TryGetValue(string.Empty, out var val))
        {
            // blank header already exists = skip
            return;
        }

        // new blank key-value
        Data[string.Empty] = string.Empty;
    }

    private void UpdateKey(KeyValuePair<string, string> existing, string newKey)
    {
        Data.Remove(existing.Key);
        Data[newKey] = existing.Value;
    }

    private void UpdateValue(KeyValuePair<string, string> existing, string newValue)
    {
        Data[existing.Key] = newValue;
    }

    private void DeleteValue(KeyValuePair<string, string> kvp)
    {
        Data.Remove(kvp.Key);
    }
}
