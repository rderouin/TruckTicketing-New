using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Enums;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketIndex : BaseTruckTicketingComponent
{
    private bool _clearActiveTicket;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<TruckTicket, SearchCriteriaModel> _results = new();

    public PagableGridView<TruckTicket> Grid { get; private set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Parameter]
    public EventCallback<TruckTicket> OnTruckTicketSelect { get; set; }
    
    [Parameter]
    public EventCallback ChildStateChange { get; set; } = EventCallback.Empty;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    protected async Task LoadTruckTickets(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        criteria.Filters.TryAdd(nameof(TruckTicket.IsDeleted), false);
        if (!criteria.OrderBy.HasText())
        {
            criteria.OrderBy = nameof(TruckTicket.LoadDate);
            criteria.SortOrder = SortOrder.Desc;
        }

        _results = await TruckTicketService.Search(criteria) ?? _results;
        _isLoading = false;

        if (_clearActiveTicket)
        {
            ViewModel.SetActiveTicketNumbers(Array.Empty<string>());
            _clearActiveTicket = false;
        }

        StateHasChanged();
    }

    protected async Task HandleTruckTicketRowSelect(TruckTicket truckTicket)
    {
        if (OnTruckTicketSelect.HasDelegate)
        {
            await OnTruckTicketSelect.InvokeAsync(truckTicket);
        }
    }

    public async Task ReloadGrid(bool clearActiveTicket = false)
    {
        _clearActiveTicket = clearActiveTicket;
        await Grid.ReloadGrid();
    }

    public List<TruckTicket> GetSelectedTickets()
    {
        return new(Grid.SelectedResults);
    }

    public void ClearSelectedTickets()
    {
        Grid.ClearSelectedResults();
    }
}
