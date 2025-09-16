using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class VolumeChangePage
{
    public PagableGridView<VolumeChange> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<VolumeChange, SearchCriteriaModel> _results = new();

    [Inject]
    private IVolumeChangeService VolumeChangeService { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task LoadVolumeChanges(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        _results = await VolumeChangeService.Search(criteria) ?? _results;
        _isLoading = false;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private async Task Export()
    {
        var exporter = new PagableGridExporter<VolumeChange>(_grid, CsvExportService);
        await exporter.Export($@"VolumeChange{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");
    }
}
