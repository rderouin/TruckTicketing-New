using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components.SalesManagement;
using SE.TruckTicketing.Client.Pages.Accounts;
using SE.TruckTicketing.Client.Pages.BillingConfig;
using SE.TruckTicketing.Client.Pages.LoadConfirmations;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.Statuses;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class LoadConfirmationsGrid : BaseTruckTicketingComponent
{
    private static HashSet<LoadConfirmationStatus> _statusesForUpdatedSignatures = new()
    {
        LoadConfirmationStatus.Open,
        LoadConfirmationStatus.PendingSignature,
        LoadConfirmationStatus.SubmittedToGateway,
        LoadConfirmationStatus.WaitingSignatureValidation,
        LoadConfirmationStatus.SignatureVerified,
        LoadConfirmationStatus.Rejected,
        LoadConfirmationStatus.WaitingForInvoice,
    };

    private dynamic _billingServiceDialog;

    private Dictionary<Guid, bool> _disablePreviewButton = new();

    private PagableGridView<LoadConfirmation> _grid;

    private bool _isLoading;

    private SearchResultsModel<LoadConfirmation, SearchCriteriaModel> _results = new();

    public Dictionary<CompositeKey<Guid>, EntityStatus> _statuses = new();

    private Timer _timer;

    private bool _timerLock;

    private IJSObjectReference JsModule;

    public SalesManagementGrid SelectedSalesManagementGrid;

    [Parameter]
    public bool HideFilter { get; set; }

    public int RecordCount { get; set; }

    public int RejectedCount { get; set; }

    public int OpenCount { get; set; }

    public int PendingCount { get; set; }

    [Parameter]
    public bool MultiSelect { get; set; }

    [Parameter]
    public EventCallback<int> OnRecordCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnRejectedCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnOpenCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnPendingCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnFieldTicketCountChange { get; set; }

    [Parameter]
    public LoadConfirmationStatus FilterStatus { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> BeforeDataLoad { get; set; }

    [Parameter]
    public EventCallback SelectionChanged { get; set; }

    [Parameter]
    public EventCallback ChildStateChanged { get; set; }

    [Parameter]
    public LoadConfirmationPage LoadConfirmationPage { get; set; }

    [Inject]
    private ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        JsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/main.js");

        if (LoadConfirmationPage != null)
        {
            LoadConfirmationPage.FacilityChangedEvent += FacilityChangeHandler;
        }

        if (MultiSelect)
        {
            _timer = new(LoadConfirmationRefreshTimer, new { }, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
        }
    }

    private async void LoadConfirmationRefreshTimer(object state)
    {
        if (_timerLock)
        {
            return;
        }

        try
        {
            _timerLock = true;

            // any LCs in the grid?
            var lcList = _grid?.Results?.Results?.ToDictionary(lc => lc.Key, lc => lc);
            if (lcList?.Any() != true)
            {
                return;
            }

            // refresh statuses for all LCs
            var prevStatuses = _statuses;
            _statuses = await LoadConfirmationService.GetLoadConfirmationStatuses(lcList.Keys.ToList());
            var currStatuses = _statuses;
            await InvokeAsync(StateHasChanged);

            // compare the states for the grid refresh
            var anyChanged = currStatuses.Count != prevStatuses.Count;
            if (!anyChanged)
            {
                foreach (var currStatus in currStatuses.Values)
                {
                    if (!prevStatuses.TryGetValue(currStatus.ReferenceEntityKey, out var prevStatus) || prevStatus == null)
                    {
                        anyChanged = true;
                        break;
                    }

                    if (currStatus.Status != prevStatus.Status)
                    {
                        anyChanged = true;
                        break;
                    }
                }
            }

            // refresh the grid if the pending status has changed
            if (anyChanged)
            {
                await _grid.ReloadGrid();
            }
        }
        finally
        {
            _timerLock = false;
        }
    }

    private void FacilityChangeHandler(object sender, EventArgs e)
    {
        _ = _grid.ReloadGrid();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private async Task OpenLoadConfirmationDialog(Guid loadConfirmationId)
    {
        var model = await LoadConfirmationService.GetById(loadConfirmationId);
        await DialogService.OpenAsync<LoadConfirmationDetails>($"Edit Load Confirmation {model.Number}", new()
        {
            { nameof(LoadConfirmationDetails.Model), model },
        }, new()
        {
            Width = "80%",
            Height = "95%",
        });
    }

    private async Task Export()
    {
        var exporter = new PagableGridExporter<LoadConfirmation>(_grid, CsvExportService);
        await exporter.Export($@"load-confirmations{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");
    }

    private bool IsLcReadyToSend(LoadConfirmation loadConfirmation)
    {
        return loadConfirmation.Status == LoadConfirmationStatus.Open && ((loadConfirmation.EndDate.HasValue && loadConfirmation.EndDate.Value.Date < DateTime.Today) ||
                                                                          (loadConfirmation.Frequency == LoadConfirmationFrequency.OnDemand.ToString() &&
                                                                           loadConfirmation.StartDate.Date < DateTime.Today));
    }

    private void ShowTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Open - End Date of this Load Confirmation is elapsed, please send this Load Confirmation.", options);
    }

    public async Task GridReloadLoadConfirmationGrid(SearchCriteriaModel criteria)
    {
        await _grid.SetExternalSearchCriteriaModel(criteria);
    }

    private bool IsPendingAsyncAction(CompositeKey<Guid> lcKey)
    {
        return MultiSelect && _statuses.TryGetValue(lcKey, out var entityStatus) && entityStatus.Status.HasText();
    }

    public async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;

        if (BeforeDataLoad.HasDelegate)
        {
            await BeforeDataLoad.InvokeAsync(criteria);
        }

        _results = await LoadConfirmationService.Search(criteria) ?? _results;
        if (MultiSelect)
        {
            _statuses = await LoadConfirmationService.GetLoadConfirmationStatuses(_results.Results.Select(lc => lc.Key).ToList());
        }

        _isLoading = false;

        await InvokeAsync(StateHasChanged);

        if (OnOpenCountChange.HasDelegate)
        {
            await OnOpenCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnRejectedCountChange.HasDelegate)
        {
            await OnRejectedCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnPendingCountChange.HasDelegate)
        {
            await OnPendingCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnRecordCountChange.HasDelegate)
        {
            await OnRecordCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnFieldTicketCountChange.HasDelegate)
        {
            await OnFieldTicketCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
    }

    private async Task OpenCustomerDialog(Guid customerId)
    {
        await DialogService.OpenAsync<AccountDetailsPage>("", new()
        {
            { nameof(AccountDetailsPage.Id), customerId.ToString() },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    public IList<LoadConfirmation> GetSelectedResults()
    {
        return _grid.SelectedResults;
    }

    public async Task ReloadGrid()
    {
        await _grid.ReloadGrid();
    }

    private void BeforeSalesLinesLoad(SearchCriteriaModel searchCriteriaModel, LoadConfirmation loadConfirmation)
    {
        searchCriteriaModel.AddFilter(nameof(SalesLine.LoadConfirmationId), loadConfirmation.Id);
        searchCriteriaModel.AddFilter(nameof(SalesLine.Status), new CompareModel
        {
            Operator = CompareOperators.ne,
            Value = SalesLineStatus.Void.ToString(),
        });
    }

    private async Task PreviewLoadConfirmation(LoadConfirmation lc)
    {
        try
        {
            _disablePreviewButton[lc.Id] = true;
            StateHasChanged();

            // do it
            var uri = await LoadConfirmationService.PreviewLoadConfirmation(lc.Key);
            await JsModule.InvokeVoidAsync("openPdf", uri);
        }
        finally
        {
            _disablePreviewButton[lc.Id] = false;
            StateHasChanged();
        }
    }

    private async Task OpenBillingDialog(LoadConfirmation loadConfirmation)
    {
        if (loadConfirmation == null || loadConfirmation.BillingConfigurationId == Guid.Empty)
        {
            return;
        }

        _billingServiceDialog = await DialogService.OpenAsync<BillingConfigurationEdit>("Billing Configuration", new()
        {
            { nameof(BillingConfigurationEdit.Id), loadConfirmation.BillingConfigurationId },
            { nameof(BillingConfigurationEdit.BillingCustomerId), loadConfirmation.BillingCustomerId.ToString() },
            { nameof(BillingConfigurationEdit.Operation), "edit" },
            { nameof(BillingConfigurationEdit.HideReturnToAccount), true },
            { nameof(BillingConfigurationEdit.AddEditBillingConfiguration), new EventCallback<BillingConfiguration>(this, UpdatedBillingConfiguration) },
            { nameof(BillingConfigurationEdit.CancelAddEditBillingConfiguration), new EventCallback<bool>(this, CloseBillingConfigDialog) },
        }, new()
        {
            Width = "80%",
            Height = "95%",
        });
    }

    private void CloseBillingConfigDialog(bool isCanceled)
    {
        DialogService.Close();
    }

    private async Task UpdatedBillingConfiguration(BillingConfiguration billingConfig)
    {
        DialogService.Close();
        await _grid.ReloadGrid();
    }

    private bool IsPreviewDisabled(LoadConfirmation lc)
    {
        return _disablePreviewButton.TryGetValue(lc.Id, out var state) && state;
    }

    private string RetrieveLCEmails(LoadConfirmation lc)
    {
        lc.SignatoryEmails = string.Join(", ", lc.Signatories.Select(sc => sc.Email));
        return lc.SignatoryEmails;
    }

    private string ApplyHighlight(LoadConfirmation lc)
    {
        var classes = new List<string>();

        if (lc.SalesLineCount == 0 &&
            lc.Status != LoadConfirmationStatus.Void)
        {
            classes.Add("redhighlight");
        }

        if (lc.SignatoriesAreUpdated &&
            _statusesForUpdatedSignatures.Contains(lc.Status))
        {
            classes.Add("font-weight-bold");
        }

        return string.Join(" ", classes);
    }

    private bool WasEverSent(DateTimeOffset dt)
    {
        return dt != default;
    }
}
