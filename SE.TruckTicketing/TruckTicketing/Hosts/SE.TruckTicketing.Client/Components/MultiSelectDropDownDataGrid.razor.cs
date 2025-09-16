using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components;

public partial class MultiSelectDropDownDataGrid<TModel, TValue> : TridentApiListBox<TModel, TValue> where TModel : class, IGuidModelBase
{
    private RadzenDropDownDataGrid<IEnumerable<TValue>> _dropDownDataGrid;

    private Dictionary<TValue, TModel> _textProperties;

    private object _value;

    [Parameter]
    public RenderFragment Columns { get; set; }

    private IEnumerable<Guid> Values => Value as IEnumerable<Guid>;

    private async Task HandleHeaderCheckboxChange(bool args)
    {
        _value = args ? Results?.Results?.Cast<IGuidModelBase>().Select(c => c.Id) : Enumerable.Empty<TValue>();
        Value = (IEnumerable<TValue>)_value;
        if (FetchModelOnItemSelect.HasDelegate)
        {
            _textProperties = new();
            if (Value != null)
            {
                foreach (var val in Value)
                {
                    var data = Results?.Results?.FirstOrDefault(x => x.Id.Equals(val));
                    _textProperties.Add(val, data);
                }
            }

            await FetchModelOnItemSelect.InvokeAsync(_textProperties);
        }
    }
}
