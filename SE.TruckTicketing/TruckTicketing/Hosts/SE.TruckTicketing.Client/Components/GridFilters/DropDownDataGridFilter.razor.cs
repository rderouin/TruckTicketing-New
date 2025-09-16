using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;
using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class DropDownDataGridFilter<TItem> : FilterComponent<string[]>
    where TItem : class, IGuidModelBase
{
    private RadzenDropDownDataGrid<object> _dropDownDataGrid;

    private SearchResultsModel<TItem, SearchCriteriaModel> _results = new();

    private object _value;

    [Parameter]
    public RenderFragment Columns { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> BeforeDataLoad { get; set; }

    [Inject]
    private IServiceProxyBase<TItem, Guid> ServiceProxy { get; set; }

    [Parameter]
    public string DefaultSortProperty { get; set; }

    [Parameter]
    public SortOrder DefaultSortOrder { get; set; } = SortOrder.Asc;

    [Parameter]
    public string TextProperty { get; set; }

    [Parameter]
    public string ValueProperty { get; set; } = nameof(GuidApiModelBase.Id);

    [Parameter]
    public Func<TItem, string> ValuePropertySelector { get; set; } = item => item.Id.ToString();

    [Parameter]
    public CompareOperators CompareOperator { get; set; } = CompareOperators.eq;

    protected List<string> Values =>
        (_value as EnumerableQuery<Guid>)?.Select(value => value.ToString()).ToList() ??
        (_value as EnumerableQuery<Guid?>)?.Select(value => value.ToString()).ToList() ??
        (_value as EnumerableQuery<string>)?.Select(value => value.ToString()).ToList();

    public override void Reset(SearchCriteriaModel criteria)
    {
        _value = default;
        _dropDownDataGrid.Reset();

        criteria?.Filters?.Remove(FilterPath);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData(new());
            StateHasChanged();
        }
    }

    protected async Task HandleChange(object args)
    {
        await PropagateFilterValueChange((args as IEnumerable<object>)?.Select(value => value.ToString()).ToArray());
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        var values = Values?.ToArray() ?? Array.Empty<string>();

        if (!values.Any())
        {
            criteria.Filters.Remove(FilterPath);
            return;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter()
                                            .StartGroup();

        var index = 0;
        foreach (var value in values)
        {
            if (query is GroupStart groupStart)
            {
                query = groupStart.AddAxiom(new()
                {
                    Key = $"{FilterPath}{++index}".Replace(".", string.Empty),
                    Field = FilterPath,
                    Operator = CompareOperator,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{FilterPath}{++index}".Replace(".", string.Empty),
                    Field = FilterPath,
                    Operator = CompareOperator,
                    Value = value,
                });
            }
        }

        criteria.Filters[FilterPath] = ((AxiomTokenizer)query).EndGroup().Build();
    }

    private async Task LoadData(LoadDataArgs args)
    {
        var criteria = args.ToSearchCriteriaModel();

        if (!string.IsNullOrWhiteSpace(DefaultSortProperty) && string.IsNullOrWhiteSpace(args.OrderBy))
        {
            criteria.OrderBy = DefaultSortProperty;
            criteria.SortOrder = DefaultSortOrder;
        }

        if (BeforeDataLoad.HasDelegate)
        {
            await BeforeDataLoad.InvokeAsync(criteria);
        }
        _results = await ServiceProxy!.Search(criteria) ?? _results;
    }
}
