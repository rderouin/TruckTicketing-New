using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Humanizer;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.TruckTicketComponents;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Acknowledgement;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Toolbelt.Blazor.HotKeys;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketIndexPage : BaseTruckTicketingComponent
{
    private Account _customer;

    private HotKeysContext _hotKeysContext;

    private bool _isDownloadingTicket;

    private bool _isSaving;

    private bool _isSplitting;

    private BillingConfigurationWatcher BillingConfigurationWatcher;

    private GenericInitializationWatcher GenericInitializationWatcher;

    private bool IsAcknowledged;

    private MaterialApprovalWatcher MaterialApprovalWatcher;

    private SalesLinesWatcher SalesLinesWatcher;

    protected NewTruckTicketStubRequestDialogForm StubRequestDialog;

    private TareWeightWatcher TareWeightWatcher;

    protected TruckTicketIndex TruckTicketIndex;

    private WellClassificationWatcher WellClassificationWatcher;

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private IServiceProxyBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    public IServiceProxyBase<Note, Guid> NotesService { get; set; }

    [Inject]
    public ISalesLineService SalesLineService { get; set; }

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private HotKeys HotKeys { get; set; }

    [Inject]
    private IServiceProxyBase<Acknowledgement, Guid> AcknowledgementService { get; set; }

    private EventCallback<TruckTicket> HandleTruckTicketVolumeChangeSubmit => new(this, OnTicketVolumeChangeRequested);

    private EventCallback HandleDiscardVolumeChanges => new(this, OnHandleDiscardVolumeChanges);

    private EventCallback OnRemoveSalesLines { get; set; }

    private TruckTicket _activeTruckTicket => ViewModel.TruckTicket;

    private TruckTicket _activeTruckTicketBackup => ViewModel.TruckTicketBackup;

    private bool HasTruckTicketWritePermission => HasWritePermission(Permissions.Resources.TruckTicket);

    protected bool DisableSaveButton
    {
        get
        {
            if (ViewModel is null)
            {
                return true;
            }

            if (!HasTruckTicketWritePermission)
            {
                return true;
            }

            if (ViewModel?.TruckTicket.FacilityId == Guid.Empty)
            {
                return true;
            }

            if (ViewModel?.HasMaterialApprovalErrors == true)
            {
                return true;
            }

            if (ViewModel?.Facility?.Type == FacilityType.Lf)
            {
                return !_activeTruckTicket.LandfillSampled && _activeTruckTicket.RequireSample;
            }

            if (ViewModel?.VolumeCutValidationErrors.Any() ?? false)
            {
                return true;
            }

            return ViewModel.IsRunningWorkflows ||
                   ViewModel.IsLoadingBillingConfigurations ||
                   ViewModel.IsLoadingSalesLines ||
                   ViewModel.IsRemovingSalesLines ||
                   ViewModel.IsTotalVolumePercentInvalid ||
                   ViewModel.IsTotalVolumeInvalid;
        }
    }

    private bool HasVolumeChanges
    {
        get
        {
            var volumesHaveChanged = _activeTruckTicket.OilVolume != _activeTruckTicketBackup.OilVolume ||
                                     _activeTruckTicket.WaterVolume != _activeTruckTicketBackup.WaterVolume ||
                                     _activeTruckTicket.SolidVolume != _activeTruckTicketBackup.SolidVolume ||
                                     _activeTruckTicket.TotalVolume != _activeTruckTicketBackup.TotalVolume;

            return volumesHaveChanged;
        }
    }

    private bool IsSplitButtonVisible =>
        _activeTruckTicket?.Id != default && (_activeTruckTicket.TruckTicketType == TruckTicketType.WT || _activeTruckTicket.TruckTicketType == TruckTicketType.SP) &&
        (_activeTruckTicket?.TicketNumber?.HasText() ?? false) &&
        _activeTruckTicketBackup?.Status is TruckTicketStatus.New or TruckTicketStatus.Stub or TruckTicketStatus.Open;

    private bool CanEditMultiple =>
        TruckTicketIndex?.GetSelectedTickets()?.Count > 1 &&
        TruckTicketIndex?.GetSelectedTickets()?.All(tt => EditMultipleTicketsViewModel.AllowedStatuses.Contains(tt.Status)) == true;

    private Func<IEnumerable<TruckTicket>, Task<IEnumerable<TruckTicket>>> HandleTruckTicketSplit => SplitTruckTicket;

    private Func<IEnumerable<TruckTicket>, Task<bool>> HandleConfirmCustomerOnTicket => ConfirmCustomerOnTicket;

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private bool ShowCloneTicket => ViewModel.TruckTicket?.TruckTicketType == TruckTicketType.WT && ViewModel.TruckTicketBackup?.Status == TruckTicketStatus.Approved;

    [Inject]
    private TruckTicketWorkflowManager WorkflowManager { get; set; }

    public override void Dispose()
    {
        ViewModel.StateChanged -= StateChange;
        ViewModel.TicketSaved -= ViewModelOnTicketSaved;
        _hotKeysContext.Dispose();
    }

    protected override void OnInitialized()
    {
        ViewModel.CloseActiveTruckTicket();
        ViewModel.StateChanged += StateChange;
        ViewModel.TicketSaved += ViewModelOnTicketSaved;
        _hotKeysContext = HotKeys.CreateContext()
                                 .Add(ModKeys.Ctrl, Keys.L, _ => HandleAddLiveScaleTicketClick())
                                 .Add(ModKeys.Ctrl, Keys.S, _ => HandleSave())
                                 .Add(ModKeys.Ctrl, Keys.Backspace, _ => ViewModel.CloseActiveTruckTicket());
    }

    private async Task ViewModelOnTicketSaved()
    {
        await TruckTicketIndex.ReloadGrid();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            ViewModel.Workflows = new ITruckTicketWorkflow[] { WellClassificationWatcher, MaterialApprovalWatcher, TareWeightWatcher, BillingConfigurationWatcher, SalesLinesWatcher };

            WorkflowManager.RegisterWorkflows(new ITruckTicketWorkflow[] { WellClassificationWatcher, MaterialApprovalWatcher, TareWeightWatcher, BillingConfigurationWatcher, SalesLinesWatcher });
        }
    }

    private async Task ActivateTruckTicket(TruckTicket truckTicket)
    {
        ViewModel.SetActiveTicketNumbers(new[] { truckTicket.TicketNumber });
        StateHasChanged();
        ViewModel.IsRefresh = false;
        ViewModel.IsRefreshMaterialApproval = false;
        ViewModel.IsSourceLocationDropDownCacheRefresh = false;
        ViewModel.IsFacilityServiceDropDownCacheRefresh = false;
        ViewModel.IsSpartanProductParameterDropDownCacheRefresh = false;
        ViewModel.IsTruckingCompanyDropDownCacheRefresh = false;

        await ViewModel.Initialize(truckTicket);
        //Call for acknowledgement based on customer credit/watchlist status
        await DisplayGeneralCreditStatusMessage(truckTicket.BillingCustomerId, true);
    }

    private async Task StateChange()
    {
        await InvokeAsync(StateHasChanged);
    }

    protected void HandleDiscardChanges()
    {
        ViewModel.SetResponse(null);
        ViewModel.SetTruckTicket(ViewModel.TruckTicketBackup);
    }

    protected async Task OpenTicketStubDialog()
    {
        await StubRequestDialog.Open();
    }

    protected async Task ReloadTruckTicketDialog()
    {
        await TruckTicketIndex.ReloadGrid();
    }

    private async Task DisplayGeneralCreditStatusMessage(Guid? accountId, bool loadNewRecord)
    {
        if (IsAcknowledged && loadNewRecord)
        {
            IsAcknowledged = false;
        }

        if (accountId == null || accountId.GetValueOrDefault(Guid.Empty) == Guid.Empty || IsAcknowledged)
        {
            return;
        }

        _customer = await AccountService.GetById(accountId.Value);

        if (_customer == null)
        {
            return;
        }

        List<Acknowledgement> _acknowledgements = new();
        var customerCreditStatus = _customer.CreditStatus switch
                                   {
                                       CreditStatus.RequiresRenewal => CreditStatus.RequiresRenewal.Humanize(),
                                       CreditStatus.Denied => CreditStatus.Denied.Humanize(),
                                       _ => string.Empty,
                                   };

        var customerWatchList = _customer.WatchListStatus switch
                                {
                                    WatchListStatus.Yellow => WatchListStatus.Yellow.Humanize(),
                                    WatchListStatus.Red => WatchListStatus.Red.Humanize(),
                                    _ => string.Empty,
                                };

        var creditStatusMessage =
            $"Customer credit status is {customerCreditStatus}.  Please alert GM, Manager, Lead Advisor, Peter Maros, Kari Nyland and SES Marketing Accounting.  Make plans to collect payment through credit card or prepayment for future loads.";

        var watchListMessage =
            $"Customer credit watchlist is {customerWatchList}.  Please alert GM, Manager, Lead Advisor, Peter Maros, Kari Nyland and SES Marketing Accounting.  Make plans to collect payment through credit card or prepayment for future loads.";

        var netOffMessage =
            "Customer has net off.  Please alert GM, Manager, Lead Advisor, Peter Maros, Kari Nyland and SES Marketing Accounting.  Make plans to collect payment through credit card or prepayment for future loads.";

        if (_customer.EnableCreditMessagingGeneral != null)
        {
            if (_customer.EnableCreditMessagingGeneral.Value)
            {
                var creditMessagingStatusTitle = $"Customer credit status: {customerCreditStatus}";
                if (!string.IsNullOrEmpty(customerCreditStatus))
                {
                    var acknowledgementConfirmed = await DialogService.Confirm(creditStatusMessage, creditMessagingStatusTitle,
                                                                               new()
                                                                               {
                                                                                   OkButtonText = "Acknowledge",
                                                                                   CancelButtonText = "Cancel",
                                                                                   ShowClose = false,
                                                                               });

                    if (acknowledgementConfirmed.GetValueOrDefault())
                    {
                        _acknowledgements.Add(new()
                        {
                            ReferenceEntityId = ViewModel.TruckTicket.Id,
                            Status = customerCreditStatus,
                            AcknowledgedBy = Application.User.Principal.Identity!.Name,
                            AcknowledgeAt = DateTimeOffset.UtcNow,
                        });
                    }
                }

                if (_customer.WatchListStatus == WatchListStatus.Yellow)
                {
                    var watchListTitle = $"Customer credit watchlist: {customerWatchList}";
                    var acknowledgementConfirmed = await DialogService.Confirm(watchListMessage, watchListTitle,
                                                                               new()
                                                                               {
                                                                                   OkButtonText = "Acknowledge",
                                                                                   CancelButtonText = "Cancel",
                                                                                   ShowClose = false,
                                                                               });

                    if (acknowledgementConfirmed.GetValueOrDefault())
                    {
                        _acknowledgements.Add(new()
                        {
                            ReferenceEntityId = ViewModel.TruckTicket.Id,
                            Status = customerWatchList,
                            AcknowledgedBy = Application.User.Principal.Identity!.Name,
                            AcknowledgeAt = DateTimeOffset.UtcNow,
                        });
                    }
                }

                if (_customer.NetOff != null)
                {
                    if (_customer.NetOff.Value)
                    {
                        var watchListTitle = "Customer has Net Off";
                        var acknowledgementConfirmed = await DialogService.Confirm(netOffMessage, watchListTitle,
                                                                                   new()
                                                                                   {
                                                                                       OkButtonText = "Acknowledge",
                                                                                       CancelButtonText = "Cancel",
                                                                                       ShowClose = false,
                                                                                   });

                        if (acknowledgementConfirmed.GetValueOrDefault())
                        {
                            _acknowledgements.Add(new()
                            {
                                ReferenceEntityId = ViewModel.TruckTicket.Id,
                                Status = "Net Off",
                                AcknowledgedBy = Application.User.Principal.Identity!.Name,
                                AcknowledgeAt = DateTimeOffset.UtcNow,
                            });
                        }
                    }
                }
            }
        }

        if (_customer.EnableCreditMessagingRedFlag != null)
        {
            if (_customer.EnableCreditMessagingRedFlag.Value && _customer.WatchListStatus == WatchListStatus.Red)
            {
                var watchListTitle = $"Customer credit watchlist: {customerWatchList}";
                var acknowledgementConfirmed = await DialogService.Confirm(watchListMessage, watchListTitle,
                                                                           new()
                                                                           {
                                                                               OkButtonText = "Acknowledge",
                                                                               CancelButtonText = "Cancel",
                                                                               ShowClose = false,
                                                                           });

                if (acknowledgementConfirmed.GetValueOrDefault())
                {
                    _acknowledgements.Add(new()
                    {
                        ReferenceEntityId = ViewModel.TruckTicket.Id,
                        Status = customerWatchList,
                        AcknowledgedBy = Application.User.Principal.Identity!.Name,
                        AcknowledgeAt = DateTimeOffset.UtcNow,
                    });
                }
            }
        }

        foreach (var ack in _acknowledgements)
        {
            await AcknowledgementService.Create(ack);
        }

        IsAcknowledged = true;
    }

    private async Task HandleStatusTransition(TruckTicketStatus newStatus)
    {
        var ticketToSave = _activeTruckTicket.Clone();
        ticketToSave.Status = newStatus;
        ticketToSave.ResetVolumeFields = false;
        await HandleSave(ticketToSave);
    }

    protected async Task HandleSave(TruckTicket truckTicketToSave = null)
    {
        try
        {
            _isSaving = true;
            StateHasChanged();

            if (HasVolumeChanges && _activeTruckTicketBackup.TruckTicketType == TruckTicketType.SP && ViewModel?.TruckTicket?.Id != Guid.Empty)
            {
                await OpenTruckTicketVolumeChangesDialog();
            }

            if (await ShouldAbortSaveForDuplicateBillOfLadingCheck() is true)
            {
                return;
            }

            // this check handles discarding changes in TruckTicketVolumeChangesDialog or clicking the X in that dialog
            if (HasVolumeChanges && _activeTruckTicket.IsTrackManualVolumeChangesType && _activeTruckTicket.ResetVolumeFields && ViewModel?.TruckTicket?.Id != Guid.Empty)
            {
                ResetVolumeFields();
                return;
            }

            if (ViewModel.HasActiveDuplicateSalesLines())
            {
                await ShowMessage("Duplicate sales lines detected",
                                  "The ticket you are attempting to save contains duplicate sales lines. Please review and remove any duplicates before saving by closing and refreshing the ticket.");

                return;
            }

            if (ViewModel.HasZeroAmountInAdditionalServices())
            {
                await ShowMessage("Zero quantity detected",
                                  "The quantities must be specified in the additional services section.");

                return;
            }

            UpdateSalesLineDataMetadata(ViewModel.SalesLines);

            //Check for threshold for Sales Lines

            if (ViewModel.SalesLines != null && ViewModel.SalesLines.Any())
            {
                var thresholdCheckRequest = new TruckTicketAssignInvoiceRequest
                {
                    TruckTicket = ViewModel.TruckTicket,
                    BillingConfigurationId = ViewModel?.TruckTicket?.BillingConfigurationId ?? default,
                    SalesTotalValue = ViewModel?.TruckTicket?.SalesTotalValue ?? default,
                    SalesLineCount = ViewModel?.SalesLines?.Count ?? 0,
                };

                var thresholdCheckResponse = await TruckTicketService.EvaluateTruckTicketInvoiceThreshold(thresholdCheckRequest);
                if (thresholdCheckResponse.HasText())
                {
                    var confirmation = await DialogService.Confirm(thresholdCheckResponse, options: new()
                    {
                        OkButtonText = "Proceed",
                        CancelButtonText = "Cancel",
                    });

                    ViewModel.InvoiceThresholdViolationMessage = thresholdCheckResponse;
                    if (confirmation != true)
                    {
                        return;
                    }
                }
            }
            else
            {
                ViewModel.InvoiceThresholdViolationMessage = default;
            }

            var request = new TruckTicketSalesPersistenceRequest
            {
                TruckTicket = truckTicketToSave ?? _activeTruckTicket,
                SalesLines = ViewModel.SalesLines.ToList(),
            };

            var response = await TruckTicketService.PersistTruckTicketAndSalesLines(request);

            if (response.IsSuccessStatusCode)
            {
                ViewModel.TruckTicket = response.Model.TruckTicket.Clone();
                ViewModel.TruckTicketBackup = response.Model.TruckTicket.Clone();
                await DisplayGeneralCreditStatusMessage(_activeTruckTicket.BillingCustomerId, false);

                NotificationService.Notify(NotificationSeverity.Success, "Success", "Ticket updated");

                ViewModel.SetActiveTicketNumbers(new[] { _activeTruckTicket.TicketNumber });
                await TruckTicketIndex.ReloadGrid();

                await ViewModel.SetSalesLines(response.Model.SalesLines, true);
                ViewModel.TriggerAfterSave();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", "An error occured while trying to save the current ticket");
            }

            ViewModel.SetResponse(response);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    public void UpdateSalesLineDataMetadata(IEnumerable<SalesLine> salesLines)
    {
        var truckTicketId = ViewModel.TruckTicket.Id;
        var sourceLocation = ViewModel.SourceLocation;
        var truckTicket = ViewModel.TruckTicket;
        foreach (var salesLine in ViewModel.SalesLines)
        {
            // TruckTicket Data
            salesLine.TruckTicketId = truckTicketId;
            salesLine.WellClassification = truckTicket.WellClassification;
            salesLine.TruckTicketDate = truckTicket.LoadDate ?? default;
            salesLine.SourceLocationId = truckTicket.SourceLocationId;
            salesLine.GeneratorId = truckTicket.GeneratorId;
            salesLine.GeneratorName = truckTicket.GeneratorName;
            salesLine.TruckTicketNumber = truckTicket.TicketNumber;
            salesLine.EdiFieldValues = truckTicket.EdiFieldValues;
            salesLine.IsEdiValid = truckTicket.IsEdiValid;
            salesLine.DowNonDow = truckTicket.DowNonDow;
            salesLine.BillOfLading = truckTicket.BillOfLading;
            salesLine.ManifestNumber = truckTicket.ManifestNumber;
            salesLine.FacilityId = truckTicket.FacilityId;

            // Facility Data
            if (ViewModel.Facility != null)
            {
                // TODO: update to pull site id off of the truck ticket once it's added to the entity/model
                salesLine.FacilitySiteId = ViewModel.Facility.SiteId;
                salesLine.BusinessUnit = ViewModel.Facility.BusinessUnitId;
                salesLine.Division = ViewModel.Facility.Division;
                salesLine.LegalEntity = ViewModel.Facility.LegalEntity;
            }

            //customer
            if (ViewModel.BillingCustomer != null || _customer != null)
            {
                salesLine.AccountNumber = _customer?.AccountNumber ?? ViewModel.BillingCustomer?.AccountNumber;
                salesLine.CustomerNumber = _customer?.CustomerNumber ?? ViewModel.BillingCustomer?.CustomerNumber;
            }

            // SourceLocation Data
            if (sourceLocation != null)
            {
                salesLine.SourceLocationId = sourceLocation.Id;
                salesLine.SourceLocationFormattedIdentifier = sourceLocation.FormattedIdentifier.HasText() ? sourceLocation.FormattedIdentifier : sourceLocation.SourceLocationName;
                salesLine.SourceLocationIdentifier = sourceLocation.Identifier;
                salesLine.SourceLocationTypeName = sourceLocation.SourceLocationTypeName;
            }

            // Account and/or Customer Data
            salesLine.CustomerId = truckTicket.BillingCustomerId;
            salesLine.CustomerName = truckTicket.BillingCustomerName;
            salesLine.BillOfLading = truckTicket.BillOfLading;
            salesLine.ManifestNumber = truckTicket.ManifestNumber;
            salesLine.MaterialApprovalId = truckTicket.MaterialApprovalId;
            salesLine.MaterialApprovalNumber = truckTicket.MaterialApprovalNumber;
            salesLine.ServiceTypeId = truckTicket.ServiceTypeId;
            salesLine.ServiceTypeName = truckTicket.ServiceType;
            salesLine.TruckingCompanyId = truckTicket.TruckingCompanyId;
            salesLine.TruckingCompanyName = truckTicket.TruckingCompanyName;
            salesLine.Attachments = truckTicket.Attachments?
                                               .Select(attachment => new SalesLineAttachment
                                                {
                                                    Container = attachment.Container,
                                                    Path = attachment.Path,
                                                    File = attachment.File,
                                                    Id = attachment.Id,
                                                })
                                               .ToList() ?? new();
        }
    }

    private async Task OpenTruckTicketVolumeChangesDialog()
    {
        await DialogService.OpenAsync<TruckTicketVolumeChangesDialog>("Volume Changes", new()
        {
            { nameof(TruckTicketVolumeChangesDialog.Model), _activeTruckTicket },
            { nameof(TruckTicketVolumeChangesDialog.OnSubmit), HandleTruckTicketVolumeChangeSubmit },
            { nameof(TruckTicketVolumeChangesDialog.OnDiscardVolumeChanges), HandleDiscardVolumeChanges },
        });
    }

    private void OnTicketVolumeChangeRequested(TruckTicket truckTicketWithVolumeChange)
    {
        _activeTruckTicket.VolumeChangeReason = truckTicketWithVolumeChange.VolumeChangeReason;
        _activeTruckTicket.VolumeChangeReasonText = truckTicketWithVolumeChange.VolumeChangeReasonText;
        DialogService.Close();
    }

    private void OnHandleDiscardVolumeChanges()
    {
        ResetVolumeFields();
        DialogService.Close();
    }

    private void ResetVolumeFields()
    {
        _activeTruckTicket.OilVolume = _activeTruckTicketBackup.OilVolume;
        _activeTruckTicket.WaterVolume = _activeTruckTicketBackup.WaterVolume;
        _activeTruckTicket.SolidVolume = _activeTruckTicketBackup.SolidVolume;
        _activeTruckTicket.TotalVolume = _activeTruckTicketBackup.TotalVolume;
        _activeTruckTicket.VolumeChangeReason = _activeTruckTicketBackup.VolumeChangeReason;
        _activeTruckTicket.VolumeChangeReasonText = _activeTruckTicketBackup.VolumeChangeReasonText;

        StateHasChanged();
    }

    protected async Task HandleDownloadTicketClick()
    {
        _isDownloadingTicket = true;
        var response = await TruckTicketService.DownloadTicket(_activeTruckTicket.Key);
        if (response.IsSuccessStatusCode)
        {
            await FileDownloadService.DownloadFile($"{_activeTruckTicket.TicketNumber}.pdf", await response.HttpContent.ReadAsByteArrayAsync(),
                                                   MediaTypeNames.Application.Pdf);
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to download complete scale ticket.");
        }

        _isDownloadingTicket = false;
    }

    protected async Task HandleAddLiveScaleTicketClick()
    {
        var truckTicket = new TruckTicket
        {
            TruckTicketType = TruckTicketType.LF,
            Source = TruckTicketSource.Manual,
            Status = TruckTicketStatus.New,
            TimeIn = DateTimeOffset.Now,
            LoadDate = DateTimeOffset.Now,
        };

        ViewModel.IsRefresh = false;
        ViewModel.IsRefreshMaterialApproval = false;
        ViewModel.IsSourceLocationDropDownCacheRefresh = false;
        ViewModel.IsFacilityServiceDropDownCacheRefresh = false;
        ViewModel.IsSpartanProductParameterDropDownCacheRefresh = false;
        ViewModel.IsTruckingCompanyDropDownCacheRefresh = false;
        ViewModel.HasMaterialApprovalErrors = false;
        await ViewModel.Initialize(truckTicket);
    }

    private async Task CloneTruckTicket()
    {
        await DialogService.OpenAsync<NewTruckTicketCloneComponent>("Truck Ticket Clone",
                                                                    new()
                                                                    {
                                                                        { nameof(NewTruckTicketCloneComponent.Model), ViewModel.TruckTicket },
                                                                    });
    }

    protected async Task HandleAddManualSpartanTicketClick()
    {
        var truckTicket = new TruckTicket
        {
            TruckTicketType = TruckTicketType.SP,
            Source = TruckTicketSource.Manual,
            Status = TruckTicketStatus.Open,
        };

        ViewModel.IsRefresh = false;
        ViewModel.IsRefreshMaterialApproval = false;
        ViewModel.IsSourceLocationDropDownCacheRefresh = false;
        ViewModel.IsFacilityServiceDropDownCacheRefresh = false;
        ViewModel.IsSpartanProductParameterDropDownCacheRefresh = false;
        ViewModel.IsTruckingCompanyDropDownCacheRefresh = false;
        await ViewModel.Initialize(truckTicket);
    }

    private async Task<IEnumerable<TruckTicket>> SplitTruckTicket(IEnumerable<TruckTicket> splitTruckTickets)
    {
        return await TruckTicketService.SplitTruckTickets(splitTruckTickets, _activeTruckTicket.Key);
    }

    private async Task SplitTicket()
    {
        _isSplitting = true;

        await DialogService.OpenAsync<TruckTicketSplitTicket>($"Split Ticket-{_activeTruckTicket.TicketNumber}", new()
                                                              {
                                                                  { nameof(TruckTicketSplitTicket.TruckTicket), _activeTruckTicket },
                                                                  { nameof(TruckTicketSplitTicket.SplitTruckTicket), HandleTruckTicketSplit },
                                                                  { nameof(TruckTicketSplitTicket.OnCancel), HandleCancel },
                                                                  { nameof(TruckTicketSplitTicket.ConfirmCustomerOnTicket), HandleConfirmCustomerOnTicket },
                                                              },
                                                              new()
                                                              {
                                                                  Width = "60%",
                                                              });

        _isSplitting = false;

        await TruckTicketIndex.ReloadGrid();
    }

    private async Task<bool> ConfirmCustomerOnTicket(IEnumerable<TruckTicket> splitTruckTicket)
    {
        var isSameCustomer = await TruckTicketService.ConfirmCustomerOnTickets(splitTruckTicket);
        return isSameCustomer;
    }

    private void TruckTicketIndexChange()
    {
        StateHasChanged();
    }

    private async Task ShowEditMultipleDialog()
    {
        // read the current state
        var selectedTickets = TruckTicketIndex.GetSelectedTickets();
        if (selectedTickets?.Any() != true)
        {
            return;
        }

        // create the model & validate
        var model = new EditMultipleTicketsViewModel(selectedTickets);
        if (model.IsValid() == false)
        {
            NotificationService.Notify(NotificationSeverity.Info, "No common properties to change.");
            return;
        }

        // open the edit dialog for a valid model
        await DialogService.OpenAsync<EditMultipleTickets>($"Editing {model.TruckTickets.Count} Tickets", new()
        {
            [nameof(EditMultipleTickets.Model)] = model,
            [nameof(EditMultipleTickets.OnSubmit)] = new EventCallback<EditMultipleTicketsViewModel>(this, OnSubmit),
            [nameof(EditMultipleTickets.OnCancel)] = new EventCallback(this, OnCancel),
        });

        async Task OnSubmit()
        {
            await UpdateMultipleTickets(model);
            DialogService.Close();
            TruckTicketIndex.ClearSelectedTickets();
        }

        void OnCancel()
        {
            DialogService.Close();
        }
    }

    private async Task UpdateMultipleTickets(EditMultipleTicketsViewModel model)
    {
        // propagate changes
        foreach (var truckTicket in model.TruckTickets)
        {
            if (model.PropertyBag.CanEditSourceLocation && model.PropertyBag.SourceLocation is { } sourceLocation)
            {
                truckTicket.SourceLocationId = sourceLocation.Id;
                truckTicket.SourceLocationName = sourceLocation.SourceLocationName;
                truckTicket.SourceLocationCode = sourceLocation.SourceLocationCode;
                truckTicket.SourceLocationFormatted = sourceLocation.FormattedIdentifier;
                truckTicket.GeneratorId = sourceLocation.GeneratorId;
                truckTicket.GeneratorName = sourceLocation.GeneratorName;

                // status is changed to 'Hold' due to a possible billing customer change on a different source location
                truckTicket.Status = TruckTicketStatus.Hold;
            }

            if (model.PropertyBag.CanEditFacilityServiceSubstance && model.PropertyBag.FacilityServiceSubstance is { } index)
            {
                truckTicket.FacilityServiceSubstanceId = index.Id;
                truckTicket.SubstanceId = index.SubstanceId;
                truckTicket.SubstanceName = index.Substance;
                truckTicket.FacilityServiceId = index.FacilityServiceId;
                truckTicket.ServiceTypeId = index.ServiceTypeId;
            }

            if (model.PropertyBag.CanEditTruckingCompany && model.PropertyBag.TruckingCompany is { } company)
            {
                truckTicket.TruckingCompanyId = company.Id;
                truckTicket.TruckingCompanyName = company.Name;
            }

            if (model.PropertyBag.CanEditWellClassification)
            {
                truckTicket.WellClassification = model.PropertyBag.WellClassification;
            }

            if (model.PropertyBag.CanEditQuadrant)
            {
                truckTicket.Quadrant = model.PropertyBag.Quadrant;
            }

            if (model.PropertyBag.CanEditLevel)
            {
                truckTicket.Level = model.PropertyBag.Level;
            }
        }

        // save all
        var anyErrors = false;
        var truckTicketSaveTasks = model.TruckTickets.Select(tt => (tt, TruckTicketService.Update(tt))).ToList();
        foreach (var (tt, task) in truckTicketSaveTasks)
        {
            try
            {
                var response = await task;
                if (!response.IsSuccessStatusCode)
                {
                    anyErrors = true;
                }
            }
            catch (Exception e)
            {
                BaseLogger.LogError(e, $"Unable to save Truck Ticket '{tt.TicketNumber}'.");
                anyErrors = true;
            }
        }

        if (anyErrors)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Errors occurred while saving tickets, some changes might not be saved.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Success, "All tickets have been updated successfully.");
        }
    }

    private async Task<bool?> ShouldAbortSaveForDuplicateBillOfLadingCheck()
    {
        var truckTicket = ViewModel.TruckTicket;
        var truckTicketBackup = ViewModel.TruckTicketBackup;

        // Do not initiate duplicate check if bill of lading or trucking company isn't specified
        if (truckTicket.TruckingCompanyId == Guid.Empty || !truckTicket.BillOfLading.HasText())
        {
            return false;
        }

        string BillOfLadingKey(TruckTicket ticket)
        {
            return $"{ticket?.TruckingCompanyId}{ticket?.BillOfLading}";
        }

        // If we do not detect a change in bill of lading or selected trucking company, skip check
        if (BillOfLadingKey(truckTicket).Equals(BillOfLadingKey(truckTicketBackup), StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        var searchCriteria = new SearchCriteriaModel<TruckTicket>();
        searchCriteria.AddFilter(nameof(TruckTicket.TruckingCompanyId), truckTicket.TruckingCompanyId);
        searchCriteria.AddFilter(nameof(TruckTicket.BillOfLading).AsCaseInsensitiveFilterKey(), truckTicket.BillOfLading);
        searchCriteria.PageSize = 1;

        var duplicateTicket = (await TruckTicketService.Search(searchCriteria)).Results?.FirstOrDefault();
        if (duplicateTicket is null)
        {
            return false;
        }

        var message =
            $"The bill of lading number ({duplicateTicket.BillOfLading}) for trucking company ({duplicateTicket.TruckingCompanyName.Trim()}) has been associated with another truck ticket number ({duplicateTicket.TicketNumber} {duplicateTicket.Status}) effective ({duplicateTicket.EffectiveDate?.ToShortDateString()}). Are you sure you want to proceed?";

        return await DialogService.Confirm(message, "Duplicate Bill of Lading Usage Detected", new()
        {
            CancelButtonText = "Proceed",
            OkButtonText = "Abort Operation",
            ShowClose = false,
        });
    }
}
