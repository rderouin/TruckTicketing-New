using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;

using Radzen;
using Radzen.Blazor;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentApiDropDown<TModel, TValue> : BaseTruckTicketingComponent
    where TModel : class, IGuidModelBase
{
    private TValue _paramValue;

    private TValue _value;

    protected RadzenDropDown<TValue> DropDown;

    protected SearchResultsModel<TModel, SearchCriteriaModel> Results = new();

    private bool _useCacheOnSearch => IsCacheRefresh;

    [Parameter]
    public EventCallback<SearchCriteriaModel> GetSearchCriteriaModel { get; set; }

    [Parameter]
    public EventCallback FinishRefreshingCache { get; set; }

    [Parameter]
    public bool IsCacheRefresh { get; set; }

    [Parameter]
    public SearchCriteriaModel PreLoadedSearchCriteriaModel { get; set; } = null;

    [Parameter]
    public bool AllowClear { get; set; }

    [Parameter]
    public bool AllowFiltering { get; set; } = true;

    [Parameter]
    public string Class { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> OnDataLoading { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string Id { get; set; }

    [Parameter]
    public EventCallback<TModel> OnItemLoad { get; set; }

    [Parameter]
    public EventCallback<TModel> OnItemSelect { get; set; }

    [Parameter]
    public EventCallback<object> OnChange { get; set; }

    [Parameter]
    public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.CaseInsensitive;

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public RenderFragment<TModel> Template { get; set; }

    [Parameter]
    public string TextProperty { get; set; }

    [Parameter]
    public bool CacheItemsById { get; set; }

    [Parameter]
    public Action<ICacheEntry> ConfigureIdentityCacheEntryOptions { get; set; }

    public TModel TypedSelectedItem { get; private set; }

    [Parameter]
    public TValue Value
    {
        get => _value;
        set => SetPropertyValue(ref _value, value, ValueChanged);
    }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public string ValueProperty { get; set; } = nameof(IGuidModelBase.Id);

    [Inject]
    private IServiceProxyBase<TModel, Guid> ServiceProxy { get; set; }

    public IEnumerable<TModel> LoadedItems => Results.Results;

    protected virtual async Task Change(object args)
    {
        TypedSelectedItem = DropDown?.SelectedItem as TModel;
        await InvokeOnItemSelect(TypedSelectedItem);

        if (OnChange.HasDelegate)
        {
            await OnChange.InvokeAsync(args);
        }
    }

    protected virtual async Task OnLoadData(LoadDataArgs args)
    {
        var criteria = args.ToSearchCriteriaModel();
        await BeforeDataLoad(criteria);
        if (PreLoadedSearchCriteriaModel != null && IsCacheRefresh)
        {
            criteria = PreLoadedSearchCriteriaModel;
        }

        IsLoading = true;
        Results = await ServiceProxy!.Search(criteria, CacheItemsById, ConfigureIdentityCacheEntryOptions)!;

        if (GetSearchCriteriaModel.HasDelegate)
        {
            await GetSearchCriteriaModel.InvokeAsync(criteria);
        }

        if (IsCacheRefresh && FinishRefreshingCache.HasDelegate)
        {
            await FinishRefreshingCache.InvokeAsync();
        }

        IsLoading = false;
    }

    public async Task RefreshCache(SearchCriteriaModel criteria)
    {
        IsLoading = true;
        StateHasChanged();
        Results = await ServiceProxy!.Search(criteria, CacheItemsById, ConfigureIdentityCacheEntryOptions, true)!;
        await OnLoadData(new());
        IsLoading = false;
        StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        SetPropertyValue(ref _paramValue, Value, new(this, EnsureBoundValueIsLoaded));
    }

    protected virtual async Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        if (OnDataLoading.HasDelegate)
        {
            await OnDataLoading.InvokeAsync(criteria);
        }
    }

    protected virtual async Task EnsureBoundValueIsLoaded()
    {
        var id = (Value as Guid?).GetValueOrDefault(Guid.Empty);
        if (id == Guid.Empty)
        {
            return;
        }

        var model = Results?.Results?.FirstOrDefault(item => item.Id.Equals(Value));
        if (model == null)
        {
            model = await ServiceProxy.GetById(id, CacheItemsById, ConfigureIdentityCacheEntryOptions);

            if (model != null)
            {
                Results = new(new[] { model }.Concat(Results?.Results ?? Array.Empty<TModel>()));
                TypedSelectedItem = model;
                await InvokeOnItemLoad(model);
            }
        }
        else
        {
            await InvokeOnItemLoad(model);
        }
    }

    public async Task Reload(int pageSize = 10)
    {
        IsLoading = true;
        StateHasChanged();

        await EnsureBoundValueIsLoaded();
        await OnLoadData(new() { Top = PageSize == 0 ? 10 : pageSize });
        IsLoading = false;
        StateHasChanged();
    }

    public virtual async Task Refresh(object args = null)
    {
        await Reload();
    }

    protected virtual async Task InvokeOnItemLoad(TModel model)
    {
        if (OnItemLoad.HasDelegate)
        {
            await OnItemLoad.InvokeAsync(model);
        }
    }

    protected virtual async Task InvokeOnItemSelect(TModel model)
    {
        if (OnItemSelect.HasDelegate)
        {
            await OnItemSelect.InvokeAsync(model);
        }
    }
}
