using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.InvoiceComponents;
using SE.TruckTicketing.Client.Components.SalesManagement;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api;
using Trident.Search;
using Trident.UI.Blazor.Components.Grid;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketSales : BaseTruckTicketingComponent
{
    private PagableGridView<SalesLine> _grid;

    private bool _showReversedSalesLines;

    private LoadConfirmation LoadConfirmationModel { get; set; }

    private Invoice InvoiceModel { get; set; }

    [Parameter]
    public EventCallback OnRemoveSalesLines { get; set; }

    [Parameter]
    public bool ShowAdditionalServicesOnly { get; set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    private ISalesLineService SalesLineService { get; set; }

    [Inject]
    public IInvoiceService InvoiceService { get; set; }

    [Inject]
    public ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    public SearchResultsModel<SalesLine, SearchCriteriaModel> SalesLineResults
    {
        get
        {
            IEnumerable<SalesLine> salesLines;

            if (ShowAdditionalServicesOnly)
            {
                salesLines = ViewModel.CombinedAdditionalServiceSalesLines;
            }
            else if (_showReversedSalesLines)
            {
                salesLines = ViewModel.ReversedSalesLines.Concat(ViewModel.SalesLines).ToArray();
            }
            else
            {
                salesLines = ViewModel.SalesLines;
            }

            return new(salesLines);
        }
    }

    public override void Dispose()
    {
        ViewModel.StateChanged -= StateChange;
        ViewModel.Initialized -= StateChange;
    }

    protected override async Task OnInitializedAsync()
    {
        var salesLine = ViewModel.SalesLines?.FirstOrDefault();

        if (salesLine != null)
        {
            if (salesLine.LoadConfirmationId != null)
            {
                var loadConfirmationId = (Guid)salesLine.LoadConfirmationId;
                await GetLoadConfirmation(loadConfirmationId);
            }

            if (salesLine.InvoiceId != null)
            {
                var invoiceId = (Guid)salesLine.InvoiceId;
                await GetInvoice(invoiceId);
            }
        }

        ViewModel.StateChanged += StateChange;
        ViewModel.Initialized += StateChange;
    }

    private async Task StateChange()
    {
        await InvokeAsync(StateHasChanged);
        await _grid.ReloadGrid();
    }

    private async Task AddAdditionalService()
    {
        await ViewModel.AddAdditionalServiceSalesLine();

        await _grid.EditRow(SalesLineResults.Results.Last());
    }

    private async Task RemoveSalesLinesFromLoadConfirmation()
    {
        // only allow sales lines to be removed from load confirmation when ticket is approved or invoiced
        if (ViewModel.TruckTicket.Status is TruckTicketStatus.Approved or TruckTicketStatus.Invoiced)
        {
            try
            {
                ViewModel.IsRemovingSalesLines = true;
                StateHasChanged();

                var truckTicketKeys = new List<CompositeKey<Guid>> { ViewModel.TruckTicket.Key }.AsEnumerable();
                var updatedSalesLines = await SalesLineService.RemoveFromLoadConfirmationOrInvoice(truckTicketKeys);

                if (updatedSalesLines.Any())
                {
                    await ViewModel.ReloadCurrentTruckTicket();
                    await OnRemoveSalesLines.InvokeAsync();
                    await ViewModel.SetSalesLines(updatedSalesLines);
                    ViewModel.TriggerAfterSave();

                    NotificationService.Notify(NotificationSeverity.Success, detail: "Sales lines removed");
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Error", "An error occured while trying to reset this ticket's sales lines");
                }
            }
            finally
            {
                ViewModel.IsRemovingSalesLines = false;
                StateHasChanged();
            }
        }
    }

    private async Task HandleProductSelect(Product product, SalesLine salesLine)
    {
        await ViewModel.AdditionalServiceProductSelectHandler(product.Name, product.Number, product.UnitOfMeasure, salesLine);
        await StateChange();
    }

    private async Task OnRateChange(double newRate, SalesLine salesLine)
    {
        await OpenSalesLinePriceChangeDialog(salesLine, salesLine.Rate, newRate);
    }

    private void OnQuantityChange(double newQuantity, SalesLine salesLine)
    {
        salesLine.Quantity = newQuantity;
        ViewModel.SetTotalValue(salesLine);
        StateHasChanged();
        ViewModel.TriggerStateChanged();
    }

    private async Task OpenSalesLinePriceChangeDialog(SalesLine salesLine, Double originalRate, Double newRate)
    {
        var salesLines = new List<SalesLine> { salesLine };
        await DialogService.OpenAsync<SalesLinePriceChangeDialog>("Price Book Customer Price Change", new()
        {
            { nameof(SalesLinePriceChangeDialog.SalesLines), salesLines },
            { nameof(SalesLinePriceChangeDialog.OriginalRate), originalRate },
            { nameof(SalesLinePriceChangeDialog.NewRate), newRate },
        });
    }

    private void BeforeLoadingProducts(SearchCriteriaModel searchCriteria)
    {
        searchCriteria.Filters[nameof(Product.LegalEntityId)] = ViewModel.Facility.LegalEntityId;

        if (ViewModel.Facility.Type == FacilityType.Lf)
        {
            var allowedCategories = new[]
            {
                ProductCategories.AdditionalServices.Lf,
                ProductCategories.AdditionalServices.Liner,
                ProductCategories.AdditionalServices.AltUnitOfMeasureClass1,
                ProductCategories.AdditionalServices.AltUnitOfMeasureClass2,
            };

            searchCriteria.Filters[nameof(Product.Categories)] = allowedCategories.AsInclusionAxiomFilter(nameof(Product.Categories).AsPrimitiveCollectionFilterKey());
        }
        else
        {
            var allowedCategories = new[] { ProductCategories.AdditionalServices.Fst };

            searchCriteria.Filters[nameof(Product.Categories)] = allowedCategories.AsInclusionAxiomFilter(nameof(Product.Categories).AsPrimitiveCollectionFilterKey());
        }

        var allowedSites = AxiomFilterBuilder
                          .CreateFilter()
                          .StartGroup()
                          .AddAxiom(new()
                           {
                               Field = nameof(Product.AllowedSites).AsPrimitiveCollectionFilterKey(),
                               Value = "",
                               Operator = CompareOperators.eq,
                               Key = "AllowedSites1",
                           })
                          .Or()
                          .AddAxiom(new()
                           {
                               Field = nameof(Product.AllowedSites).AsPrimitiveCollectionFilterKey(),
                               Value = ViewModel.TruckTicket.SiteId,
                               Operator = CompareOperators.contains,
                               Key = "AllowedSites2",
                           })
                          .EndGroup()
                          .Build();

        searchCriteria.Filters[nameof(Product.AllowedSites).AsPrimitiveCollectionFilterKey()] = allowedSites;
    }

    private async Task HandleShowReversedSalesLinesChange(bool value)
    {
        _showReversedSalesLines = value;
        await _grid.ReloadGrid();
    }

    private async Task OpenInvoiceDialog(Guid invoiceId)
    {
        var invoice = await InvoiceService.GetById(invoiceId);
        await DialogService.OpenAsync<InvoiceDetails>($"Invoice {invoice.ProformaInvoiceNumber}", new()
        {
            { nameof(InvoiceDetails.Model), invoice },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private string GetStatusLineStyle(SalesLineStatus status)
    {
        return status switch
               {
                   SalesLineStatus.Preview => "text-warning",
                   SalesLineStatus.Approved => "text-success",
                   SalesLineStatus.Exception => "text-danger",
                   SalesLineStatus.SentToFo => "text-warning",
                   SalesLineStatus.Posted => "text-warning",
                   _ => "text-secondary",
               };
    }

    private async Task GetLoadConfirmation(Guid loadConfirmationId)
    {
        LoadConfirmationModel = await LoadConfirmationService.GetById(loadConfirmationId);
    }

    private async Task GetInvoice(Guid invoiceId)
    {
        InvoiceModel = await InvoiceService.GetById(invoiceId);
    }
}
