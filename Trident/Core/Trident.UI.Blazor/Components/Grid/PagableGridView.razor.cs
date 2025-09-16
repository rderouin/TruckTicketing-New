using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Newtonsoft.Json;

using Radzen;
using Radzen.Blazor;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid.ColumnOptions;

namespace Trident.UI.Blazor.Components.Grid;

public interface IPagableGridView
{
}

public class GridRowEventArgs<TModel>
{
    public PagableGridView<TModel> Grid { get; set; }

    public TModel RowData { get; set; }
}

public partial class PagableGridView<TModel> : BaseRazorComponent, IPagableGridView
{
    private readonly List<ColumnDefinition<TModel>> _columns = new();

    private readonly int[] _pageSizes = { 5, 10, 25, 50, 100 };

    private ColumnOptionsDialog _columnOptionsDialog;

    private SearchCriteriaModel _exportCriteria;

    private SearchCriteriaModel _externalSearchCriteriaModel;

    private SearchResultsModel<TModel, SearchCriteriaModel> _results = new();

    public RadzenDataGrid<TModel> Grid;

    private string keyWord = string.Empty;

    private Func<TModel, bool> RowExpansionFunc;

    [Parameter]
    public object ChildKey { get; set; }

    [Parameter]
    public int GridPageSize { get; set; } = 10;

    [Parameter]
    public string ColumnWidth { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, dynamic> Attributes { get; set; }

    [Parameter]
    public bool EnableFilters { get; set; } = true;

    public bool FilterVisible { get; set; }

    public bool ShowFilters { get; set; }

    public IEnumerable<ColumnDefinition<TModel>> ColumnDefinitionList => _columns;

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool EnableSearch { get; set; } = true;

    [Parameter]
    public RenderFragment Columns { get; set; }

    [Parameter]
    public RenderFragment CustomButton { get; set; }

    [Parameter]
    public Type ItemType { get; set; }

    [Parameter]
    public bool EnablePaging { get; set; }

    [Parameter]
    public bool EnableSorting { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> OnDataLoad { get; set; }

    [Parameter]
    public EventCallback<GridRowEventArgs<TModel>> OnChildDataLoad { get; set; }

    [Parameter]
    public EventCallback<TModel> RowClick { get; set; }

    [Parameter]
    public string ExportFileName { get; set; }

    [Parameter]
    public string ImportConfigurationName { get; set; }

    [Parameter]
    public Func<SearchCriteriaModel, Task<IEnumerable>> OnExport { get; set; }

    [Parameter]
    public RenderFragment<TModel> Template { get; set; }

    [CascadingParameter(Name = nameof(ParentGrid))]
    public IPagableGridView ParentGrid { get; set; }

    [Parameter]
    public bool EnableMultiLineSelect { get; set; }

    [Parameter]
    public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Single;

    [Parameter]
    public DataGridEditMode EditMode { get; set; } = DataGridEditMode.Single;

    [Parameter]
    public SearchResultsModel<TModel, SearchCriteriaModel> Results
    {
        get => _results;
        set
        {
            _results = value;
            keyWord = _results.Info.Keywords;

            // Synchronize uncontrolled current page with grid current page
            if (_results?.Info?.CurrentPage != null && Grid?.CurrentPage != null &&
                _results?.Info?.CurrentPage != Grid?.CurrentPage)
            {
                Grid!.CurrentPage = _results!.Info!.CurrentPage!.Value;
            }

            // refresh the export criteria
            var c = _results?.Info?.Clone();
            if (c != null)
            {
                var criteria = c;
                criteria.PageSize = int.MaxValue;
                _exportCriteria = criteria;
            }
            else
            {
                _exportCriteria = null;
            }
        }
    }

    [Parameter]
    public Expression<Func<TModel, bool>> RowExpansionExpression { get; set; }

    [Parameter]
    public bool EnableRowExpansion { get; set; }

    [Parameter]
    public bool EnableExport { get; set; } = true;

    [Parameter]
    public EventCallback ChildStateChange { get; set; } = EventCallback.Empty;

    public IList<TModel> SelectedResults { get; set; } = new List<TModel>();

    private bool MultiSelectHeaderCheckBoxValue => Results?.Results.Any(i => SelectedResults != null && SelectedResults.Contains(i)) ?? false;

    private string CssClasses =>
        Attributes?.ContainsKey("class") ?? false
            ? Attributes["class"]
            : string.Empty;

    public bool ShowExport => EnableExport && ExportFileName != null && OnExport != null;

    private int EndPageCount
    {
        get
        {
            var pageEnd = Start + PageSize;
            var result = pageEnd < Count ? pageEnd : Count;
            return result;
        }
    }

    protected int PageSize
    {
        get => Results?.Info?.PageSize.GetValueOrDefault() ?? 10;
        set
        {
            if (Results.Info == null)
            {
                return;
            }

            if (Equals(Results.Info.PageSize.GetValueOrDefault(), value))
            {
                return;
            }

            Results.Info.PageSize = value;
            Results.Info.CurrentPage = 0;
            InvokeAsync(StateHasChanged);
        }
    }

    protected int Start => GetStart();

    protected int Count => Results?.Info?.TotalRecords ?? 0;

    public void ShowColumnOptions()
    {
        _columnOptionsDialog.ShowColumnOptions();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (RowExpansionExpression != null)
        {
            RowExpansionFunc = RowExpansionExpression.Compile();
        }
    }

    public void AddColumn(ColumnDefinition<TModel> column)
    {
        _columns.Add(column);
    }

    public void RemoveColumn(ColumnDefinition<TModel> column)
    {
        _columns.Remove(column);
    }

    private async Task LoadData(LoadDataArgs args)
    {
        if (OnDataLoad.HasDelegate)
        {
            await OnDataLoad.InvokeAsync(MapNew(args, Results.Info));
        }

        ClearSelectedResults();
    }

    private void PagerSizeChanged(object state)
    {
        Grid.Reload();
    }

    private void PreviousClicked(MouseEventArgs args)
    {
        if (Start - PageSize >= 0)
        {
            Results.Info.CurrentPage--;
        }

        Grid.Reload();
    }

    private void NextClicked(MouseEventArgs args)
    {
        if (Start + PageSize < Count)
        {
            Results.Info.CurrentPage++;
        }

        Grid.Reload();
    }

    public async Task EditRow(TModel item)
    {
        await Grid.EditRow(item);
    }

    public async Task UpdateRow(TModel item)
    {
        await Grid.UpdateRow(item);
    }

    public void CancelEditRow(TModel item)
    {
        Grid.CancelEditRow(item);
    }

    public void OnSearchKeyUp(KeyboardEventArgs args)
    {
        if (args.Code is "Enter" or "NumpadEnter")
        {
            OnSearchButtonClick();
        }
    }

    private void OnSearchButtonClick()
    {
        Results.Info.CurrentPage = 0;
        Grid.Reload();
    }

    public async Task OnFilterPanelCriteriaChanged(SearchResultInfoModel<SearchCriteriaModel> criteria)
    {
        Results.Info = criteria;
        await Grid.Reload();
    }

    private async Task OnRowClick(DataGridRowMouseEventArgs<TModel> args)
    {
        if (RowClick.HasDelegate)
        {
            await RowClick.InvokeAsync(args.Data);
        }
    }

    private async Task AddOrRemoveToSelectedResults(TModel item, bool selected)
    {
        if (SelectedResults.Contains(item) && !selected)
        {
            SelectedResults.Remove(item);
        }

        if (!SelectedResults.Contains(item) && selected)
        {
            SelectedResults.Add(item);
        }

        await ChildStateChange.InvokeAsync();
    }

    private async Task MultiSelectHeaderOnChange(bool args)
    {
        SelectedResults = args ? Results?.Results.ToList() : new();
        await ChildStateChange.InvokeAsync();
    }

    private int GetStart()
    {
        if (Results?.Info == null)
        {
            return 0;
        }

        var result = Results.Info.CurrentPage.GetValueOrDefault() * Results.Info.PageSize.GetValueOrDefault();
        return result;
    }

    private string GetKeywords()
    {
        return Results?.Info?.Keywords ?? string.Empty;
    }

    private void SetKeywords(string value)
    {
        if (Results?.Info != null)
        {
            Results.Info.Keywords = value;
        }
    }

    private Task OnFilterButtonClick()
    {
        if (!FilterVisible)
        {
            FilterVisible = true;
        }

        ShowFilters = !ShowFilters;

        return Task.CompletedTask;
    }

    private SearchResultInfoModel<SearchCriteriaModel> MapNew(LoadDataArgs args,
                                                              SearchResultInfoModel<SearchCriteriaModel> info)
    {
        var cloned = Clone(info);
        cloned.MultiOrderBy.Clear();

        if (_externalSearchCriteriaModel is null)
        {
            foreach (var filter in args.Filters)
            {
                cloned.Filters[filter.Property] = filter.FilterValue;
            }
        }
        else
        {
            cloned.Filters = _externalSearchCriteriaModel.Filters;
            cloned.Keywords = _externalSearchCriteriaModel.Keywords;
        }

        if (args.Sorts.Count() > 1)
        {
            foreach (var sort in args.Sorts)
            {
                cloned.MultiOrderBy[sort.Property] = sort.SortOrder == SortOrder.Ascending
                                                         ? Trident.Contracts.Enums.SortOrder.Asc
                                                         : Trident.Contracts.Enums.SortOrder.Desc;
            }
        }
        else if (!args.Sorts.Any())
        {
            if (_externalSearchCriteriaModel is not null)
            {
                cloned.OrderBy = _externalSearchCriteriaModel.OrderBy;
                cloned.SortOrder = _externalSearchCriteriaModel.SortOrder;
            }
        }
        else
        {
            var firstSort = args.Sorts.FirstOrDefault();
            cloned.OrderBy = firstSort?.Property ?? string.Empty;
            cloned.SortOrder = firstSort != null && firstSort.SortOrder == SortOrder.Descending
                                   ? Trident.Contracts.Enums.SortOrder.Desc
                                   : Trident.Contracts.Enums.SortOrder.Asc;
        }

        if (!string.Equals(keyWord, info.Keywords, StringComparison.OrdinalIgnoreCase))
        {
            cloned.CurrentPage = 0;
        }

        cloned.PageSize = args.Top;
        cloned.CurrentPage = Grid.CurrentPage;

        return cloned;
    }

    private async Task RowExpanding(TModel model)
    {
        if (OnChildDataLoad.HasDelegate)
        {
            await OnChildDataLoad.InvokeAsync(new()
            {
                Grid = this,
                RowData = model,
            });
        }
    }

    private void RowRender(RowRenderEventArgs<TModel> args)
    {
        //need better condition
        args.Expandable = EnableRowExpansion
                       && (RowExpansionFunc == null || RowExpansionFunc(args.Data));
    }

    private async Task<IEnumerable> ExportGetData()
    {
        if (_exportCriteria != null)
        {
            var task = OnExport.Invoke(_exportCriteria);
            var result = await task;
            return result;
        }

        return null;
    }

    private T Clone<T>(T target)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(target));
    }

    public async Task ReloadGrid()
    {
        if (Grid != null)
        {
            await Grid.Reload();
        }
    }

    public async Task SetExternalSearchCriteriaModel(SearchCriteriaModel searchCriteriaModel)
    {
        _externalSearchCriteriaModel = searchCriteriaModel.Clone();
        Grid.CurrentPage = 0;
        await ReloadGrid();
    }

    public void ClearSelectedResults()
    {
        SelectedResults = new List<TModel>();
    }
}
