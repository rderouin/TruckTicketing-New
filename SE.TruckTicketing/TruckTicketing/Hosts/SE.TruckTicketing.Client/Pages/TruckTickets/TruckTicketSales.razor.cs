using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.SalesManagement;
using SE.TruckTicketing.Client.Components.TruckTicketComponents;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketSales : BaseTruckTicketingComponent
{
    private PagableGridView<SalesLine> _grid;

    [CascadingParameter(Name = "TruckTicket")]
    public TruckTicket TruckTicket { get; set; }

    [Parameter]
    public BillingConfiguration BillingConfiguration { get; set; } = new BillingConfiguration();
    
    [Parameter]
    public SearchResultsModel<SalesLine, SearchCriteriaModel> SalesLineResults { get; set; }
    
    [Inject]
    public ISalesLineService SalesLineService { get; set; }
    
    [Inject]
    private IBillingConfigurationService BillingConfigurationService { get; set;}
    
    [Parameter]
    public EventCallback LoadSalesLines { get; set; }
    
    [Parameter]
    public EventCallback<TruckTicketAdditionalService> NewAdditionalServiceAdded { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAdditionalService> OnAdditionalServiceChange { get; set; }

    public bool IsRateOverriden { get; set; } = false;

    public bool ShowReversed { get; set; } = false;

    protected override async void OnInitialized()
    {
        if (TruckTicket?.BillingConfigurationId != null)
        {
            BillingConfiguration = await BillingConfigurationService.GetById(TruckTicket.BillingConfigurationId.Value);
        }
        
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        SalesLineResults.Results = SalesLineResults.Results.Where(s => ShowReversed || (s.IsReversal == false && s.IsReversed == false));
        base.OnParametersSet();
    }

    private async Task OnRateChange(double newRate ,SalesLine salesLine)
    {
        await OpenSalesLinePriceChangeDialog(salesLine, salesLine.Rate, newRate);
    }

    private void RemoveSalesLine(SalesLine salesLine)
    {
        var results = SalesLineResults.Results.Where((source, index) => source.Id != salesLine.Id);
        SalesLineResults.Results = results;
    }

    /// <summary>
    /// Below will be refactored when AdditionalService is removed from TruckTicket and made into a SalesLine
    /// </summary>
    private EventCallback<TruckTicketAdditionalService> AddAdditionalServiceHandler =>
        new(this, (Func<TruckTicketAdditionalService, Task>)(async model =>
                                                             {
                                                                 DialogService.Close();
                                                                 await NewAdditionalServiceAdded.InvokeAsync(model);
                                                             }));

    private EventCallback<TruckTicketAdditionalService> UpdateAdditionalServiceHandler =>
        new(this, (Func<TruckTicketAdditionalService, Task>)(async model =>
                                                             {
                                                                 DialogService.Close();
                                                                 await OnAdditionalServiceChange.InvokeAsync(model);
                                                             }));
    
    private async Task AddAdditionalService()
    {
        await OpenAddAdditionalServiceDialog(new(), true);
    }

    private async Task OpenSalesLinePriceChangeDialog(SalesLine salesLine, Double originalRate, Double newRate)
    {
        var salesLines = new List<SalesLine> { salesLine };
        await DialogService.OpenAsync<SalesLinePriceChangeDialog>("Price Book Customer Price Change", new()
        {
            { nameof(SalesLinePriceChangeDialog.SalesLines), salesLines},
            { nameof(SalesLinePriceChangeDialog.OriginalRate), originalRate },
            { nameof(SalesLinePriceChangeDialog.NewRate), newRate },
        });
    }
    
    private async Task OpenAddAdditionalServiceDialog(TruckTicketAdditionalService truckTicketAdditionalServiceModel, bool isNew)
    {

        await DialogService.OpenAsync<AdditionalServiceEdit>("Additional Service",
                                                             new()
                                                             {
                                                                 { "TruckTicketAdditionalService", truckTicketAdditionalServiceModel },
                                                                 { "IsNewRecord", isNew },
                                                                 { "AdditionalServices", TruckTicket.AdditionalServices },
                                                                 { "Model", TruckTicket },
                                                                 { nameof(AdditionalServiceEdit.AddAdditionalService), AddAdditionalServiceHandler },
                                                                 { nameof(AdditionalServiceEdit.UpdateAdditionalService), UpdateAdditionalServiceHandler },
                                                                 { nameof(AdditionalServiceEdit.OnCancel), HandleCancel },
                                                             });
    }
    
    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private async Task OnShowReversed()
    {
        StateHasChanged();
        await _grid.ReloadGrid();
    }
}
