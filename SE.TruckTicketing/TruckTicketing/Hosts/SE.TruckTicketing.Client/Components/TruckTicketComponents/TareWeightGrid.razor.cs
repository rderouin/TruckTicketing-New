using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;

using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class TareWeightGrid : BaseRazorComponent
{
    private PagableGridView<TruckTicketTareWeight> _grid;

    private SearchResultsModel<TruckTicketTareWeight, SearchCriteriaModel> _tareWeights = new();

    [Parameter]
    public TruckTicket Model { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    [Inject]
    private IServiceBase<TruckTicketTareWeight, Guid> TruckTicketTareWeightService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    private async Task LoadTareWeight(SearchCriteriaModel current)
    {
        current.SortOrder = SortOrder.Desc;
        current.OrderBy = nameof(TruckTicketTareWeight.LoadDate);

        var response = await TruckTicketTareWeightService.Search(current);
        _tareWeights = response;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    private async Task UploadTareWeight()
    {
        await DialogService.OpenAsync<TareWeightUploadComponent>("Upload Tare Weight Information", new()
                                                                 {
                                                                     { "TruckTicketModel", Model },
                                                                     { nameof(TareWeightUploadComponent.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                 },
                                                                 new()
                                                                 {
                                                                     Width = "60%",
                                                                 });

        await _grid.ReloadGrid();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
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
