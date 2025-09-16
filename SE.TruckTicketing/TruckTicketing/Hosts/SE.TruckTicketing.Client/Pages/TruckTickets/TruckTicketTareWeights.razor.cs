using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Components.TruckTicketComponents;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketTareWeights
{
    private PagableGridView<TruckTicketTareWeight> _grid;

    private GridFiltersContainer _gridFiltersContainer;

    private bool _isLoading;

    private SearchResultsModel<TruckTicketTareWeight, SearchCriteriaModel> _tareWeights = new();

    [Inject]
    private IServiceBase<TruckTicketTareWeight, Guid> TruckTicketTareWeightService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    private async Task LoadTareWeights(SearchCriteriaModel current)
    {
        _isLoading = true;
        current.SortOrder = SortOrder.Desc;
        current.OrderBy = nameof(TruckTicketTareWeight.LoadDate);
        _tareWeights = await TruckTicketTareWeightService.Search(current) ?? _tareWeights;
        _isLoading = false;
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFiltersContainer.Reload();
        }
    }

    protected async Task UploadTareWeight()
    {
        await DialogService.OpenAsync<TareWeightUploadComponent>("Upload Tare Weight Information", new()
                                                                 {
                                                                     //{ "TruckTicketModel", Model },
                                                                     { nameof(TareWeightUploadComponent.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                 },
                                                                 new()
                                                                 {
                                                                     Width = "60%",
                                                                 });

        await _grid.ReloadGrid();
    }

    private async Task ChangeActivateFlag(TruckTicketTareWeight model)
    {
        var response = await TruckTicketTareWeightService.Update(model);
        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: $"Activate Status changed to {model.IsActivated}.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Unable to change Activate Status.");
        }
    }

    private async Task Export()
    {
        var exporter = new PagableGridExporter<TruckTicketTareWeight>(_grid, CsvExportService);
        await exporter.Export($"truck-ticket-tare-weight-{DateTime.UtcNow:dd-MM-yyyy-hh-mm-ss}.csv");
    }
}
