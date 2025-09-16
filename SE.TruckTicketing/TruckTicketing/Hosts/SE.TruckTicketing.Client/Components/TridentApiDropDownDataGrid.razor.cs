using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentApiDropDownDataGrid<TModel, TValue> : TridentApiDropDown<TModel, TValue> where TModel : class, IGuidModelBase
{
    private bool _firstLoad;

    protected RadzenDropDownDataGrid<TValue> DropDownDataGrid;

    protected bool IsBusy;

    protected RadzenDropDownDataGrid<object> MultiSelectDropDownDataGrid;

    public object MultiSelectValue;

    protected List<TModel> Data { get; set; } = new();

    protected int Count { get; set; }

    [Parameter]
    public RenderFragment Columns { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    protected List<string> Values =>
        (MultiSelectValue as EnumerableQuery<Guid>)?.Select(value => value.ToString()).Distinct().ToList() ??
        (MultiSelectValue as EnumerableQuery<Guid?>)?.Select(value => value.ToString()).Distinct().ToList() ??
        (MultiSelectValue as EnumerableQuery<string>)?.Select(value => value.ToString()).Distinct().ToList();

    [Parameter]
    public Func<TModel, string> ValuePropertySelector { get; set; } = item => item.Id.ToString();

    public IReadOnlyCollection<TModel> View => Data.AsReadOnly();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (EqualityComparer<TValue>.Default.Equals(Value, default) || Value as Guid? == Guid.Empty)
            {
                _firstLoad = true;
                await OnLoadData(new() { Top = PageSize });
                StateHasChanged();
            }
        }
    }

    protected override async Task Change(object args)
    {
        if (!Multiple)
        {
            await OnItemSelect.InvokeAsync(DropDownDataGrid?.SelectedItem as TModel);
        }

        if (OnChange.HasDelegate)
        {
            await OnChange.InvokeAsync(args);
        }
    }

    public async Task ClearSearchText()
    {
        await DropDownDataGrid.Reload();
    }

    protected override async Task EnsureBoundValueIsLoaded()
    {
        await base.EnsureBoundValueIsLoaded();
        EnsureBoundValueIsIncludedInData();
    }

    protected override async Task OnLoadData(LoadDataArgs args)
    {
        await base.OnLoadData(args);
        EnsureBoundValueIsIncludedInData();
        StateHasChanged();
    }

    protected virtual void EnsureBoundValueIsIncludedInData()
    {
        Data = Results?.Results?.ToList() ?? Data;

        if (TypedSelectedItem is not null && Data.All(item => item.Id != TypedSelectedItem.Id))
        {
            Data.Add(TypedSelectedItem);
        }

        Count = Results?.Info?.TotalRecords ?? Math.Max(0, Data.Count);
    }

    protected override async Task InvokeOnItemLoad(TModel model)
    {
        await base.InvokeOnItemLoad(model);

        if (Count == 0 && !_firstLoad)
        {
            _firstLoad = true;
            await OnLoadData(new() { Top = PageSize });
        }

        StateHasChanged();
    }

    protected void HandleHeaderCheckboxChange(bool args)
    {
        MultiSelectValue = args ? MultiSelectDropDownDataGrid.View.Cast<IGuidModelBase>().Select(c => c.Id) : Enumerable.Empty<string>();
    }
}
