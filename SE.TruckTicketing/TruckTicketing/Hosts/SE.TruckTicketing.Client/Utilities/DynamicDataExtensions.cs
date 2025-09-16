using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Utilities;

public static class DynamicDataExtensions
{
    public static List<ExpandoObject> AsExpandoObjects<TItem>(this IEnumerable<TItem> items)
    {
        return JsonConvert.DeserializeObject<List<ExpandoObject>>(items.ToJson(), new ExpandoObjectConverter());
    }

    public static IEnumerable<dynamic> SelectForExport<TItem>(this IEnumerable<ExpandoObject> data, IEnumerable<ColumnDefinition<TItem>> columns)
    {
        return data.Select(item => SelectForExport(item, columns));
    }

    public static dynamic SelectForExport<TItem>(this ExpandoObject item, IEnumerable<ColumnDefinition<TItem>> columns)
    {
        var row = (IDictionary<string, object>)new ExpandoObject();
        var source = (IDictionary<string, object>)item;

        foreach (var column in columns.Where(col => !col.HideColumn))
        {
            if (source.TryGetValue(column.Property, out var value))
            {
                var exportValue = column.ExportAs is null ? value?.ToString() : column.ExportAs(value);
                row.TryAdd(column.Title, exportValue);
            }
        }

        return row;
    }
}
