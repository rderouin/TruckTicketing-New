using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentApiListBox<TModel, TValue> : BaseTruckTicketingComponent
    where TModel : class, IGuidModelBase
{
    private Dictionary<TValue, TModel> _textProperties;

    private IEnumerable<TValue> _value;

    protected RadzenListBox<IEnumerable<TValue>> ListBox;

    public SearchResultsModel<TModel, SearchCriteriaModel> Results = new();

    [Parameter]
    public bool AllowClear { get; set; }

    [Parameter]
    public bool AllowFiltering { get; set; }

    [Parameter]
    public string Class { get; set; }

    [Parameter]
    public string Style { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> OnDataLoading { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string Id { get; set; }

    [Parameter]
    public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.CaseInsensitive;

    [Parameter]
    public EventCallback<TModel> OnItemLoad { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<TModel>> OnItemSelect { get; set; }

    [Parameter]
    public EventCallback<Dictionary<TValue, TModel>> FetchModelOnItemSelect { get; set; }

    [Parameter]
    public EventCallback<bool> AllRecordsSelected { get; set; }

    [Parameter]
    public int PageSize { get; set; }

    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public RenderFragment<TModel> Template { get; set; }

    [Parameter]
    public string TextProperty { get; set; }

    public IEnumerable<TModel> TypedSelectedItem { get; private set; }

    [Parameter]
    public IEnumerable<TValue> Value
    {
        get => _value;
        set => SetPropertyValue(ref _value, value, ValueChanged);
    }

    [Parameter]
    public Func<TValue, (TValue, string)> FetchText { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<TValue>> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<TModel>> GetAllData { get; set; }

    [Parameter]
    public EventCallback<bool> LoadingCompleted { get; set; }

    [Parameter]
    public string ValueProperty { get; set; } = nameof(IGuidModelBase.Id);

    [Inject]
    private IServiceBase<TModel, Guid> ServiceProxy { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await OnLoadData(new());
        await base.OnInitializedAsync();
    }

    protected virtual async Task Change(object args)
    {
        if (FetchModelOnItemSelect.HasDelegate)
        {
            _textProperties = new();
            if (_value != null)
            {
                foreach (var val in _value)
                {
                    var data = Results?.Results?.FirstOrDefault(x => x.Id.Equals(val));
                    _textProperties.Add(val, data);
                }
            }

            await FetchModelOnItemSelect.InvokeAsync(_textProperties);
        }

        TypedSelectedItem = _value as IEnumerable<TModel>;
        if (AllRecordsSelected.HasDelegate)
        {
            await AllRecordsSelected.InvokeAsync(_value == null || _value?.Count() == Results?.Results?.Count() || (_value != null && !_value.Any()));
        }

        await InvokeOnItemSelect(TypedSelectedItem);
    }

    protected virtual async Task OnLoadData(LoadDataArgs args)
    {
        await LoadingCompleted.InvokeAsync(false);
        var criteria = args.ToSearchCriteriaModel();
        await BeforeDataLoad(criteria);
        Results = await ServiceProxy!.Search(criteria)!;
        if (GetAllData.HasDelegate)
        {
            await GetAllData.InvokeAsync(Results?.Results);
        }

        if (AllRecordsSelected.HasDelegate)
        {
            await AllRecordsSelected.InvokeAsync(_value.Count() == Results?.Results?.Count() || !_value.Any());
        }

        await LoadingCompleted.InvokeAsync(true);
    }

    protected virtual async Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        if (OnDataLoading.HasDelegate)
        {
            await OnDataLoading.InvokeAsync(criteria);
        }
    }

    protected virtual async Task InvokeOnItemSelect(IEnumerable<TModel> model)
    {
        if (OnItemSelect.HasDelegate)
        {
            await OnItemSelect.InvokeAsync(model);
        }
    }

    public async Task ReloadData()
    {
        await OnLoadData(new());
    }
}
