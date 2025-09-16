using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;

using Newtonsoft.Json;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.SalesManagement;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Api.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Pages.SalesManagement;

public partial class SalesManagementIndexPage : BaseTruckTicketingComponent
{
    private RadzenDropDownDataGrid<IEnumerable<Guid>> _facilityDataGrid;

    private List<Facility> _facilityRecords = new();

    private bool _isSaving;

    private SalesManagementGrid _salesLinePriceChangeGrid;

    private SalesManagementGrid _salesManagementGrid;

    private int _selectedTab;

    public SearchResultsModel<Facility, SearchCriteriaModel> facilityResults = new();

    private IJSObjectReference JsModule;

    [Parameter]
    public string FacilityIdsUrl { get; set; }

    [Parameter]
    public IEnumerable<Guid> FacilityIds { get; set; }

    public Guid FacilityId { get; set; }

    private int AllCount { get; set; }

    private int SalesLinesWithManualPriceChangeCount { get; set; }

    private string AllCountText => $"All ({AllCount})";

    private string ManualPriceChangeText => $"Manual Price Change ({SalesLinesWithManualPriceChangeCount})";

    private bool ShowFilters { get; } = true;

    private bool AdHocLoadConfirmationEnabled
    {
        get
        {
            if (_salesManagementGrid == null || !_salesManagementGrid.SelectedSalesLines.Any())
            {
                return false;
            }

            var count = _salesManagementGrid.SelectedSalesLines.GroupBy(n => n.FacilityId).Count();
            if (count > 1)
            {
                return false;
            }

            count = _salesManagementGrid.SelectedSalesLines.GroupBy(n => n.CustomerId).Count();
            if (count > 1)
            {
                return false;
            }

            var startDate = _salesManagementGrid.SelectedSalesLines.OrderBy(a => a.TruckTicketDate).First().TruckTicketDate;
            var endDate = _salesManagementGrid.SelectedSalesLines.OrderByDescending(a => a.TruckTicketDate).First().TruckTicketDate;
            return !((endDate - startDate).TotalDays > 60);
        }
    }

    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    private bool DisablePriceChanges => (_salesManagementGrid?.SelectedSalesLines ?? Array.Empty<SalesLine>()).Any(line => line.Status is SalesLineStatus.Posted);

    private List<string> FilteredSalesLineStatus => Enum.GetValues<SalesLineStatus>().ToList().Where(x => x != SalesLineStatus.Preview).Select(x => x.ToString()).ToList();

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<SalesLineEmailViewModel> HandleGenerateAdHocLoadConfirmation => new(this, GenerateAdHocLoadConfirmation);

    private EventCallback<SalesLineEmailViewModel> HandleSendAdHocLoadConfirmation => new(this, SendAdHocLoadConfirmation);

    public event EventHandler FacilityChangedEvent;

    protected override async Task OnInitializedAsync()
    {
        FacilityIds = FacilityIdsUrl != null ? JsonConvert.DeserializeObject<IEnumerable<Guid>>(HttpUtility.UrlDecode(FacilityIdsUrl)) : FacilityIds;
        await LoadFacilityData(new());
        JsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/main.js");
    }

    private void HandleFacilityChange(object args)
    {
        FacilityChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadFacilityData(LoadDataArgs args)
    {
        var searchCriteriaModel = args.ToSearchCriteriaModel();
        BeforeFacilityLoad(searchCriteriaModel);
        facilityResults = await FacilityService!.Search(searchCriteriaModel)!;
        _facilityRecords = facilityResults?.Results?.ToList();
    }

    private async Task ApplyFilterOnTabChange(SearchCriteriaModel model)
    {
        await _salesManagementGrid.GridReloadSalesLines(model);
        await _salesLinePriceChangeGrid.GridReloadSalesLines(model);
    }

    private void AllCountHandler(int count)
    {
        AllCount = count;
    }

    private void SalesLinePriceChangeCountHandler(int count)
    {
        SalesLinesWithManualPriceChangeCount = count;
    }

    private void BeforeFacilityLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Facility.IsActive)] = true;
        criteria.PageSize = Int32.MaxValue;
    }

    private void LoadAllData(SearchCriteriaModel criteria)
    {
        HandleFacilityFilter(criteria);

        if (!criteria.Filters.ContainsKey(nameof(SalesLine.Status)) && !criteria.Filters.ContainsKey(PreviewSalesLinesFilter.Key))
        {
            criteria.Filters[nameof(SalesLine.Status)] = new CompareModel
            {
                Operator = CompareOperators.ne,
                Value = SalesLineStatus.Preview.ToString(),
            };
        }

        criteria.AddFilterIf(criteria.Filters.ContainsKey(AwaitingRemovalAckFilter.Key),
                             nameof(SalesLine.AwaitingRemovalAcknowledgment), true);
    }

    private void LoadSalesLinesWithManualPriceChanged(SearchCriteriaModel criteria)
    {
        HandleFacilityFilter(criteria);

        criteria.Filters[nameof(SalesLine.IsRateOverridden)] = true;

        criteria.AddFilterIf(!criteria.Filters.ContainsKey(nameof(SalesLine.Status)),
                             nameof(SalesLine.Status), new CompareModel
                             {
                                 Operator = CompareOperators.ne,
                                 Value = SalesLineStatus.Preview.ToString(),
                             });

        criteria.AddFilterIf(criteria.Filters.ContainsKey(AwaitingRemovalAckFilter.Key),
                             nameof(SalesLine.AwaitingRemovalAcknowledgment), true);
    }

    private void HandleFacilityFilter(SearchCriteriaModel criteria)
    {
        var values = FacilityIds?.ToArray() ?? Array.Empty<Guid>();

        if (!values.Any())
        {
            criteria.Filters.Remove(nameof(SalesLine.FacilityId));
        }

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();

        var index = 0;

        foreach (var id in values)
        {
            if (query is GroupStart groupstart)
            {
                query = groupstart.AddAxiom(new()
                {
                    Key = (nameof(SalesLine.FacilityId) + ++index).Replace(".", string.Empty),
                    Field = nameof(SalesLine.FacilityId),
                    Operator = Trident.Search.CompareOperators.eq,
                    Value = id,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = (nameof(SalesLine.FacilityId) + ++index).Replace(".", string.Empty),
                    Field = nameof(SalesLine.FacilityId),
                    Operator = Trident.Search.CompareOperators.eq,
                    Value = id,
                });
            }

            criteria.Filters[nameof(SalesLine.FacilityId)] = ((AxiomTokenizer)query).EndGroup().Build();
        }
    }

    private void ChangeTabs(int selectedTab)
    {
        _selectedTab = selectedTab;
    }

    private async Task HandlePriceRefresh()
    {
        const string msg = "Are you sure you want to refresh prices?";
        const string title = "Confirm Price Refresh";
        var priceRefreshConfirm = await DialogService.Confirm(msg, title,
                                                              new()
                                                              {
                                                                  OkButtonText = "Confirm",
                                                                  CancelButtonText = "Cancel",
                                                              });

        if (priceRefreshConfirm.GetValueOrDefault())
        {
            var success = await _salesManagementGrid.RefreshPrice();

            if (success)
            {
                NotificationService.Notify(NotificationSeverity.Info, "Prices Refreshed");
            }
        }
    }

    private async Task HandleSave(bool isResendAck = false)
    {
        _isSaving = true;
        var successMessage = isResendAck ? "Successfully Republished Sales Lines to receive Removal Acknowledgement/s" : "Sales Line Saved";
        var savedSalesLines = _salesManagementGrid.SelectedSalesLines?.Any() ?? false
                                  ? await _salesManagementGrid.SaveSelectedSalesLines(isResendAck)
                                  : await _salesManagementGrid.SaveModifiedSalesLines(isResendAck);

        if (!savedSalesLines.IsNullOrEmpty())
        {
            await _salesLinePriceChangeGrid.ReloadGrid();
            NotificationService.Notify(NotificationSeverity.Success, detail: successMessage);
        }

        _isSaving = false;
    }

    private async Task HandleBulkPriceChange()
    {
        await OpenBulkSalesLinePriceChangeDialog(_salesManagementGrid.SelectedSalesLines);
    }

    private async Task OpenBulkSalesLinePriceChangeDialog(IEnumerable<SalesLine> salesLines)
    {
        await DialogService.OpenAsync<SalesLinePriceChangeDialog>("Change Unit Price", new()
        {
            { nameof(SalesLinePriceChangeDialog.IsMultiSelect), true },
            { nameof(SalesLinePriceChangeDialog.SalesLines), salesLines },
        });
    }

    private async Task OpenAdHocLoadConfirmation()
    {
        await DialogService.OpenAsync<AdHocLoadConfirmationDialog>("Ad Hoc Load Confirmation", new()
        {
            { nameof(AdHocLoadConfirmationDialog.SalesLines), _salesManagementGrid.SelectedSalesLines },
            { nameof(AdHocLoadConfirmationDialog.OnCancel), HandleCancel },
            { nameof(AdHocLoadConfirmationDialog.OnHandleGenerateAdHocLoadConfirmation), HandleGenerateAdHocLoadConfirmation },
            { nameof(AdHocLoadConfirmationDialog.OnHandleSendAdHocLoadConfirmation), HandleSendAdHocLoadConfirmation },
        });
    }

    private async Task GenerateAdHocLoadConfirmation(SalesLineEmailViewModel viewModel)
    {
        var response = await _salesManagementGrid.GenerateAdHocLoadConfirmation(viewModel);
        if (response.IsSuccessStatusCode)
        {
            var pdfBytes = await response.HttpContent.ReadAsByteArrayAsync();
            await JsModule.InvokeVoidAsync("openPdfBytes", Convert.ToBase64String(pdfBytes));
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to generate ad hoc Load Confirmation.");
        }
    }

    private async Task SendAdHocLoadConfirmation(SalesLineEmailViewModel viewModel)
    {
        try
        {
            var response = await _salesManagementGrid.SendAdHocLoadConfirmation(viewModel);
            if (response.IsSuccessStatusCode)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Ad hoc Load Confirmation sent.");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Failed to send ad hoc Load Confirmation.");
            }
        }
        catch (Exception e)
        {
            // only notify the user
            NotificationService.Notify(NotificationSeverity.Error, e.Message);
        }
    }

    private void ShowAdHocLoadConfirmationTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Select sales lines for a single facility and single customer. Ticket Date Range limited to 1 - 2 months.", options);
    }
}
