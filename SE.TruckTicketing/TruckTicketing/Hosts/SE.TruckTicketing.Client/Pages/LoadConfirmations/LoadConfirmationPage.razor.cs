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

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.LoadConfirmationComponents;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Api;
using Trident.Search;
using Trident.UI.Blazor.Components.Forms;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;
using CompareOperators = Trident.Api.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Pages.LoadConfirmations;

public partial class LoadConfirmationPage : BaseTruckTicketingComponent
{
    private readonly IEnumerable<int?> _selectableYears = new List<int?>() //the Biz wants 15 years.
    {
        null,
        2023,
        2024,
        2025,
        2026,
        2027,
        2028,
        2029,
        2030,
        2031,
        2032,
        2033,
        2034,
        2035,
        2036,
        2037,
    };

    private RadzenDropDownDataGrid<IEnumerable<Guid>> _facilityDataGrid;

    private List<Facility> _facilityRecords = new();

    public SearchResultsModel<Facility, SearchCriteriaModel> facilityResults = new();

    private IJSObjectReference JsModule;

    [Parameter]
    public string FacilityIdsUrl { get; set; }

    [Parameter]
    public IEnumerable<Guid> FacilityIds { get; set; }

    [Parameter]
    public string LoadConfirmationNumber { get; set; }

    private int RecordCount { get; set; }

    private LoadConfirmationsGrid _mainGrid { get; set; }

    private int RejectedCount { get; set; }

    private int OpenCount { get; set; }

    private int PendingCount { get; set; }

    private int FieldTicketCount { get; set; }

    private bool IsBusy { get; set; }

    private bool IsSendLoadConfirmationBusy { get; set; }

    private bool IsResendLoadConfirmationSignatureEmailBusy { get; set; }

    private bool IsResendFieldTicketsBusy { get; set; }

    private bool IsApproveSignatureBusy { get; set; }

    private bool IsRejectSignatureBusy { get; set; }

    private bool IsMarkLoadConfirmationAsReadyBusy { get; set; }

    private bool IsVoidLoadConfirmationBusy { get; set; }

    private string BusyText { get; set; }

    private bool DisableBulkDownloadButton { get; set; }

    private bool CanPreviewLoadConfirmation { get; set; }

    private bool CanDoBulkActions { get; set; }

    private string AllCountText => $"All {RecordCount}";

    private string RejectedCountText => $"Rejected {RejectedCount}";

    private string PendingCountText => $"Pending {PendingCount}";

    private string OpenCountText => $"Open {OpenCount}";

    private string FieldTicketCountText => $"Field Tickets {FieldTicketCount}";

    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    private IAccountService AccountEntityService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    private SelectOption[] LoadFrequenceDropDown =>
        new SelectOption[]
        {
            new()
            {
                Id = "Monthly",
                Text = "Monthly",
            },
            new()
            {
                Id = "Weekly",
                Text = "Weekly",
            },
        };

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

    private static void BeforeCustomerFilterDataLoad(SearchCriteriaModel criteria)
    {
        var theKey = nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!;

        criteria.Filters[theKey] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.Customer.ToString(),
        };

        criteria.Filters[nameof(Account.IsAccountActive)] = true;
    }

    private static void BeforeGeneratorFilterDataLoad(SearchCriteriaModel criteria)
    {
        var theKey = nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!;

        criteria.Filters[theKey] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.Generator.ToString(),
        };

        criteria.Filters[nameof(Account.IsAccountActive)] = true;
    }

    private async Task ApplyFilterOnTabChange(SearchCriteriaModel model)
    {
        await _mainGrid.GridReloadLoadConfirmationGrid(model);
    }

    private void BeforeFacilityLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Facility.IsActive)] = true;
        criteria.PageSize = Int32.MaxValue;
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

    private void LoadAllData(SearchCriteriaModel criteria)
    {
        if (LoadConfirmationNumber.HasText())
        {
            criteria.Filters[nameof(LoadConfirmation.Number)] = LoadConfirmationNumber;
        }
        else
        {
            criteria.Filters.Remove(nameof(LoadConfirmation.Number));
        }

        HandleFacilityFilter(criteria);
    }

    private void RecordCountHandler(int count)
    {
        RecordCount = count;
    }

    public async Task RemoveSalesLinesFromLoadConfirmation()
    {
        try
        {
            IsBusy = true;
            await InvokeAsync(StateHasChanged);

            var salesLinesToRemove = _mainGrid.SelectedSalesManagementGrid.SelectedSalesLines;
            var truckTicketIds = salesLinesToRemove.Select(s => new CompositeKey<Guid>(s.TruckTicketId, null)).ToHashSet();

            var updatedSalesLines = await _mainGrid.SelectedSalesManagementGrid.RemoveFromLoadConfirmationOrInvoice(truckTicketIds);

            if (!updatedSalesLines.IsNullOrEmpty())
            {
                NotificationService.Notify(NotificationSeverity.Success, detail: "Sales Lines Removed");
                await _mainGrid.SelectedSalesManagementGrid.ReloadGrid();
            }
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void LoadConfirmationsGridSelectionChanged()
    {
        var selection = _mainGrid.GetSelectedResults();
        CanPreviewLoadConfirmation = selection?.Count == 1;
        CanDoBulkActions = selection?.Count > 0;
        StateHasChanged();
    }

    public bool CanExecuteAction(LoadConfirmationAction action)
    {
        var selection = _mainGrid?.GetSelectedResults();
        if (!(selection?.Count > 0))
        {
            return false;
        }

        if (selection?.Count != 1 && action == LoadConfirmationAction.ResendAdvancedLoadConfirmationSignatureEmail)
        {
            return false;
        }

        // validate transitions
        var selectedLoadConfirmations = selection.Select(lc => new
        {
            LoadConfirmation = lc,
            IsActionAllowed = LoadConfirmationTransitions.IsAllowed(lc.Status, action),
        }).ToList();

        return selectedLoadConfirmations.All(lc => lc.IsActionAllowed);
    }

    private async Task BulkActionClick(LoadConfirmationAction action)
    {
        try
        {
            SetBusy(action, true);

            // any selected?
            var selection = _mainGrid.GetSelectedResults();
            if (selection == null || selection.Count < 1)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "No Load Confirmations are selected.");
                return;
            }

            // validate transitions
            var selectedLoadConfirmations = selection.Select(lc => new
            {
                LoadConfirmation = lc,
                IsActionAllowed = LoadConfirmationTransitions.IsAllowed(lc.Status, action),
            }).ToList();

            // are all permitted?
            if (selectedLoadConfirmations.Any(lc => !lc.IsActionAllowed))
            {
                var lcNumbers = string.Join(", ", selectedLoadConfirmations.Where(lc => !lc.IsActionAllowed).Select(lc => lc.LoadConfirmation.Number));
                var message = $"This action cannot be done for the selected {selectedLoadConfirmations.Count} Load Confirmations.{Environment.NewLine}Numbers: {lcNumbers}";
                NotificationService.Notify(NotificationSeverity.Warning, message);
                return;
            }

            // check if all LCs allowed for the selected action
            if (selectedLoadConfirmations.Any(lc => !lc.IsActionAllowed))
            {
                // prompt for partial bulk action
                var confirmed = await DialogService.Confirm("The selected action cannot be executed for the selected Load Confirmations. Proceed with partial bulk action?",
                                                            "Confirm partial bulk action",
                                                            new()
                                                            {
                                                                OkButtonText = "Proceed",
                                                                CancelButtonText = "Cancel",
                                                            }) == true;

                // if there are any LCs that do not support the transition and a user declined partial bulk action, then abort the op
                if (confirmed == false)
                {
                    return;
                }
            }

            // prompt for a comment if needed
            var comment = string.Empty;
            if (action == LoadConfirmationAction.RejectSignature ||
                action == LoadConfirmationAction.VoidLoadConfirmation)
            {
                // ask to comment on the action
                var model = new LoadConfirmationReasonViewModel { ShowReason = action == LoadConfirmationAction.RejectSignature };
                await DialogService.OpenAsync<LoadConfirmationReasonModal>("Provide a reason for the action",
                                                                           new()
                                                                           {
                                                                               [nameof(LoadConfirmationReasonModal.Model)] = model,
                                                                               [nameof(LoadConfirmationReasonModal.OnSubmit)] = new EventCallback(this, () => DialogService.Close()),
                                                                               [nameof(LoadConfirmationReasonModal.OnCancel)] = new EventCallback(this, () => DialogService.Close()),
                                                                           });

                // cancel clicked, cancel bulk op
                if (model.IsOkToProceed == false)
                {
                    return;
                }

                // get the fully formatted comment
                comment = model.GetFormattedComment();

                // add extra text to the comment
                switch (action)
                {
                    case LoadConfirmationAction.RejectSignature:
                        comment = $"Load Confirmation has been Rejected.{Environment.NewLine}" + comment;
                        break;
                    case LoadConfirmationAction.VoidLoadConfirmation:
                        comment = $"Load Confirmation has been Voided.{Environment.NewLine}" + comment;
                        break;
                }
            }

            // list of allowed load confirmations and corresponding request
            var allowedLoadConfirmations = selectedLoadConfirmations.Where(lc => lc.IsActionAllowed).Select(lc => lc.LoadConfirmation).ToList();
            var request = new LoadConfirmationBulkRequest
            {
                LoadConfirmationKeys = allowedLoadConfirmations.Select(lc => lc.Key).ToHashSet().ToList(),
                Action = action,
                AdditionalNotes = comment,
            };

            // request for additional info for the Advanced Load Confirmation Signature Email
            if (action == LoadConfirmationAction.ResendAdvancedLoadConfirmationSignatureEmail)
            {
                if (selection.Count == 1)
                {
                    var success = await UpdateRequestForAdvancedLoadConfirmationSignatureEmail(request, allowedLoadConfirmations, this);
                    if (!success)
                    {
                        return;
                    }
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Only a single Load Confirmation is allowed for the selected action.");
                    return;
                }
            }

            // execute the bulk action for the selected LCs which are permitted for the bulk action
            var response = await LoadConfirmationService.DoLoadConfirmationBulkAction(request);

            // update the pending status on the LCs 
            var pendingActionsLookup = response.GetPendingActionsLookup();
            foreach (var lc in selectedLoadConfirmations)
            {
                if (pendingActionsLookup.TryGetValue(lc.LoadConfirmation.Key, out var isPending))
                {
                    if (_mainGrid._statuses.TryGetValue(lc.LoadConfirmation.Key, out var status))
                    {
                        status.Status = isPending ? "PendingAsyncAction" : null;
                    }
                }
            }

            // show toast
            if (response.IsSuccessful)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Successfully queued actions for the selected load confirmations.");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Unable to queue actions for the selected load confirmations. Please, refresh the grid and try again.");
            }

            // the action might have affected the statuses of the LCs, refresh the grid
            await _mainGrid.ReloadGrid();
        }
        finally
        {
            SetBusy(action, false);
        }

        static async Task<bool> UpdateRequestForAdvancedLoadConfirmationSignatureEmail(LoadConfirmationBulkRequest request, List<LoadConfirmation> loadConfirmations, LoadConfirmationPage page)
        {
            // only 1 is allowed
            if (loadConfirmations.Count != 1)
            {
                return false;
            }

            // the subject
            var loadConfirmation = loadConfirmations.First();

            // account
            var billingCustomer = await page.AccountEntityService.GetById(loadConfirmation.BillingCustomerId);
            if (billingCustomer == null)
            {
                return false;
            }

            // list of contacts for the form
            var signatoryFunction = AccountContactFunctions.FieldSignatoryContact.ToString();
            var signatories = billingCustomer.Contacts.Where(c => c.IsActive && c.Email.HasText() && c.ContactFunctions.Contains(signatoryFunction)).ToList();
            var selectableContacts = signatories.Select(c => new DisplayEmailAddress
            {
                DisplayName = c.DisplayName,
                Email = c.Email,
                IsDefault = loadConfirmation.Signatories.Any(s => s.Email == c.Email),
            }).ToList();

            // ask to comment on the action
            var model = new AdvancedEmailViewModel
            {
                Contacts = selectableContacts,
                ContactDropdownLabel = "Signatory Contact",
            };

            // request additional info
            await page.DialogService.OpenAsync<AdvancedEmailComponent>("Add Additional Information",
                                                                       new()
                                                                       {
                                                                           [nameof(AdvancedEmailComponent.Model)] = model,
                                                                           [nameof(AdvancedEmailComponent.OnSubmit)] =
                                                                               new EventCallback<AdvancedEmailViewModel>(page, () => page.DialogService.Close()),
                                                                           [nameof(AdvancedEmailComponent.OnCancel)] = new EventCallback(page, () => page.DialogService.Close()),
                                                                       }, new()
                                                                       {
                                                                           Width = "80%",
                                                                           Height = "auto",
                                                                       });

            // cancel clicked, cancel bulk op
            if (model.IsOkToProceed == false)
            {
                return false;
            }

            // update the request
            request.IsCustomeEmail = true;
            request.To = model.To;
            request.Cc = model.Cc;
            request.Bcc = model.Bcc;
            request.AdditionalNotes = model.AdHocNote;

            return true;
        }
    }

    private void SetBusy(LoadConfirmationAction action, bool state)
    {
        IsBusy = state;
        // update the busy text
        switch (action)
        {
            case LoadConfirmationAction.SendLoadConfirmation:
                IsSendLoadConfirmationBusy = state;
                BusyText = state ? "Sending Load Confirmations..." : "";
                break;
            case LoadConfirmationAction.ResendAdvancedLoadConfirmationSignatureEmail:
                IsResendLoadConfirmationSignatureEmailBusy = state;
                BusyText = state ? "Sending Advanced Signature Emails..." : "";
                break;
            case LoadConfirmationAction.ResendLoadConfirmationSignatureEmail:
                IsResendLoadConfirmationSignatureEmailBusy = state;
                BusyText = state ? "Sending Signature Emails..." : "";
                break;
            case LoadConfirmationAction.ResendFieldTickets:
                IsResendFieldTicketsBusy = state;
                BusyText = state ? "Sending Field Tickets..." : "";
                break;
            case LoadConfirmationAction.ApproveSignature:
                IsApproveSignatureBusy = state;
                BusyText = state ? "Approving Signatures..." : "";
                break;
            case LoadConfirmationAction.RejectSignature:
                IsRejectSignatureBusy = state;
                BusyText = state ? "Rejecting Signatures..." : "";
                break;
            case LoadConfirmationAction.MarkLoadConfirmationAsReady:
                IsMarkLoadConfirmationAsReadyBusy = state;
                BusyText = state ? "Marking Ready..." : "";
                break;
            case LoadConfirmationAction.VoidLoadConfirmation:
                IsVoidLoadConfirmationBusy = state;
                BusyText = state ? "Voiding Load Confirmations..." : "";
                break;
            case LoadConfirmationAction.Unknown:
                IsBusy = state;
                BusyText = "";
                break;
        }

        ;
        StateHasChanged();
    }

    private async Task RemoveLoadConfirmationFilter()
    {
        if (LoadConfirmationNumber.HasText())
        {
            LoadConfirmationNumber = null;
        }

        await _mainGrid.ReloadGrid();
    }

    private bool IsBulkDownloadDisabled()
    {
        return DisableBulkDownloadButton || _mainGrid?.GetSelectedResults()?.Any() != true;
    }

    private async Task BulkDownloadLoadConfirmations()
    {
        try
        {
            DisableBulkDownloadButton = true;
            StateHasChanged();

            var loadConfirmations = _mainGrid?.GetSelectedResults();
            if (loadConfirmations?.Any() == true)
            {
                var tasks = new List<Task>();

                foreach (var loadConfirmation in loadConfirmations)
                {
                    var task = DownloadLoadConfirmation(loadConfirmation);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            DisableBulkDownloadButton = false;
            StateHasChanged();
        }
    }

    private async Task DownloadLoadConfirmation(LoadConfirmation loadConfirmation)
    {
        var uri = await LoadConfirmationService.DownloadLoadConfirmation(loadConfirmation.Key);
        await JsRuntime.InvokeVoidAsync("open", uri, "_blank");
    }
}
