using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.TruckTicketComponents;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketIndexPage : BaseTruckTicketingComponent
{
    private readonly TruckTicketStubsCreationDialogViewModel _truckTicketStubCreationDialogViewModel = new();

    private TruckTicket _activeTruckTicket;

    private TruckTicket _activeTruckTicketBackup;

    private bool _isDownloadingTicket;

    private bool _isSaving;

    private bool _isSpliting;

    private SearchResultsModel<SalesLine, SearchCriteriaModel> _salesLineResults = new() { Results = new List<SalesLine>() };

    private TruckTicketIndex _truckTicketIndex;

    private IEnumerable<string> markedItems = Array.Empty<string>();

    private bool _disableSaveButton
    {
        get
        {
            if (_activeTruckTicket.FacilityType == FacilityType.Lf)
            {
                return !_activeTruckTicket.LandfillSampled && _activeTruckTicket.RequireSample;
            }

            return false;
        }
    }

    private bool _splitButtonVisible =>
        _activeTruckTicket?.Id != default && (_activeTruckTicket.TruckTicketType == TruckTicketType.WT || _activeTruckTicket.TruckTicketType == TruckTicketType.SP) &&
        _activeTruckTicket.Status is TruckTicketStatus.New or TruckTicketStatus.Stub or TruckTicketStatus.Open;

    private bool _hasVolumeChanges
    {
        get
        {
            if (_activeTruckTicketBackup == null)
            {
                return false;
            }

            if (_activeTruckTicket.OilVolume != _activeTruckTicketBackup.OilVolume ||
                _activeTruckTicket.WaterVolume != _activeTruckTicketBackup.WaterVolume ||
                _activeTruckTicket.SolidVolume != _activeTruckTicketBackup.SolidVolume ||
                _activeTruckTicket.TotalVolume != _activeTruckTicketBackup.TotalVolume)
            {
                return true;
            }

            return false;
        }
    }

    [Inject]
    private IServiceProxyBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ISalesLineService SalesLineService { get; set; }

    [Inject]
    public IServiceProxyBase<Note, Guid> NotesService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback HandleDiscardVolumeChanges => new(this, OnHandleDiscardVolumeChanges);

    private EventCallback<TruckTicketStubCreationRequest> HandleTruckTicketStubCreationSubmit => new(this, OnTicketStubCreateRequested);

    private EventCallback<TruckTicket> HandleTruckTicketVolumeChangeSubmit => new(this, OnTicketVolumeChangeRequested);

    private Func<IEnumerable<TruckTicket>, Task<IEnumerable<TruckTicket>>> HandleTruckTicketSplit => SplitTruckTicket;

    private Func<IEnumerable<TruckTicket>, Task<bool>> HandleConfirmCustomerOnTicket => ConfirmCustomerOnTicket;

    private void RefreshTruckTicket()
    {
        StateHasChanged();
    }

    private async void HandleTruckTicketRowSelect(TruckTicket truckTicket)
    {
        _activeTruckTicket = truckTicket.Clone();
        _activeTruckTicketBackup = truckTicket.Clone();
        markedItems = new[] { truckTicket.TicketNumber };
        await LoadSalesLines();
    }

    private void HandleDiscardChanges()
    {
        _activeTruckTicket = _activeTruckTicketBackup.Clone();
    }

    private void HandleDetailsPaneClose()
    {
        _activeTruckTicket = default;
        _activeTruckTicketBackup = default;
    }

    private void HandleAddTicketClick()
    {
        _activeTruckTicket = new();
        _activeTruckTicketBackup = default;
        _salesLineResults = new();
    }

    private async Task OpenTruckTicketStubCreationDialog()
    {
        _truckTicketStubCreationDialogViewModel.Response = null;
        await DialogService.OpenAsync<TruckTicketStubsCreationDialog>("Create Pre-Printed Truck Tickets", new()
        {
            { nameof(TruckTicketStubsCreationDialog.ViewModel), _truckTicketStubCreationDialogViewModel },
            { nameof(TruckTicketStubsCreationDialog.OnSubmit), HandleTruckTicketStubCreationSubmit },
            { nameof(TruckTicketStubsCreationDialog.OnCancel), HandleCancel },
        });
    }

    private async Task OpenTruckTicketVolumeChangesDialog()
    {
        await DialogService.OpenAsync<TruckTicketVolumeChangesDialog>("Volume Changes", new()
        {
            { nameof(TruckTicketVolumeChangesDialog.Model), _activeTruckTicket },
            { nameof(TruckTicketVolumeChangesDialog.OnSubmit), HandleTruckTicketVolumeChangeSubmit },
            { nameof(TruckTicketVolumeChangesDialog.OnDiscardVolumeChanges), HandleDiscardVolumeChanges }, //HandleDiscardVolumeChanges
        });
    }

    private async Task OnTicketStubCreateRequested(TruckTicketStubCreationRequest stubCreationRequest)
    {
        var response = await TruckTicketService.CreateTruckTicketStubs(stubCreationRequest);

        if (response.IsSuccessStatusCode)
        {
            await _truckTicketIndex.ReloadGrid();

            if (stubCreationRequest.GeneratePdf)
            {
                await FileDownloadService.DownloadFile($"ticket-stubs-{DateTimeOffset.Now:yy-MM-dd-hh}.pdf", await response.HttpContent.ReadAsByteArrayAsync(), MediaTypeNames.Application.Pdf);
            }

            NotificationService.Notify(NotificationSeverity.Success, "Successfully created truck ticket stubs.");
            DialogService.Close();
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to create truck ticket stubs.");
        }

        _truckTicketStubCreationDialogViewModel.Response = response;
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
    }

    private void OnTicketVolumeChangeRequested(TruckTicket truckTicketWithVolumeChange)
    {
        _activeTruckTicket.VolumeChangeReason = truckTicketWithVolumeChange.VolumeChangeReason;
        _activeTruckTicket.VolumeChangeReasonText = truckTicketWithVolumeChange.VolumeChangeReasonText;
        DialogService.Close();
    }

    private async Task HandleSave()
    {
        try
        {
            await DisplayRedFlagWarningMessage(_activeTruckTicket.BillingCustomerId);

            if (_hasVolumeChanges && _activeTruckTicket.IsTrackManualVolumeChangesType)
            {
                await OpenTruckTicketVolumeChangesDialog();
            }

            // this check handles discarding changes in TruckTicketVolumeChangesDialog or clicking the X in that dialog
            if (_hasVolumeChanges && _activeTruckTicket.IsTrackManualVolumeChangesType && _activeTruckTicket.ResetVolumeFields)
            {
                ResetVolumeFields();
                return;
            }

            _isSaving = true;
            var response = string.IsNullOrEmpty(_activeTruckTicket.TicketNumber) ? await TruckTicketService.Create(_activeTruckTicket) : await TruckTicketService.Update(_activeTruckTicket);

            if (response.IsSuccessStatusCode)
            {
                _activeTruckTicket = response.Model.Clone();
                await AddNotes();
                _activeTruckTicketBackup = response.Model.Clone();

                await SaveSalesLines();

                NotificationService.Notify(NotificationSeverity.Success, "Success", "Ticket updated");
                await _truckTicketIndex.ReloadGrid();
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task AddNotes()
    {
        if (_activeTruckTicket?.MaterialApprovalId != default && _activeTruckTicket?.MaterialApprovalId != _activeTruckTicketBackup?.MaterialApprovalId)
        {
            await NotesService.Create(new()
            {
                ThreadId = $"TruckTicket|{_activeTruckTicket.Id}",
                Comment = $"Scale operator notes for material approval number {_activeTruckTicket.MaterialApprovalNumber} has been acknowledged by {_activeTruckTicket.Acknowledgement}",
                NotEditable = true,
            });
        }
    }

    private async Task DisplayRedFlagWarningMessage(Guid? accountId)
    {
        if (accountId.GetValueOrDefault(Guid.Empty) != Guid.Empty)
        {
            var account = await AccountService.GetById(accountId.Value);
            if (account.EnableCreditMessagingRedFlag != null)
            {
                if (account.EnableCreditMessagingRedFlag.Value)
                {
                    await
                        DialogService.Confirm("This customer is Red Flagged.  Please alert GM, Manager, Lead Advisor, Peter Maros, Kari Nyland and SES Marketing Accounting.  Make plans to collect payment through credit card or prepayment for future loads.",
                                              "Account has been Red Flagged", new()
                                              {
                                                  OkButtonText = "OK",
                                                  CancelButtonText = "Cancel",
                                              });
                }
            }
        }
    }

    private async Task HandleTicketDownloadAction(RadzenSplitButtonItem item)
    {
        _isDownloadingTicket = true;

        if (item == null)
        {
            await HandleDownloadScaleTicketClick();
        }

        _isDownloadingTicket = false;
    }

    private async Task HandleDownloadScaleTicketClick()
    {
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
    }

    private async Task LoadSalesLines()
    {
        _salesLineResults = new() { Results = new List<SalesLine>() };

        // truck ticket has not been populated w/ fields required to generate or save sales lines
        if (!CanGeneratePreviewSales())
        {
            return;
        }

        // truck ticket has not been saved yet - load preview sales lines
        if (_activeTruckTicket.Id == Guid.Empty)
        {
            await LoadPreviewSalesLines();
        }

        // truck ticket has been saved - query for existing sales lines
        if (_salesLineResults.Results.IsNullOrEmpty())
        {
            _salesLineResults = await SalesLineService.Search(new()
            {
                MultiOrderBy = new() { { "SalesLineNumber", SortOrder.Asc } },
                Filters = new() { { "TruckTicketId", _activeTruckTicket.Id } },
            });

            // truck ticket has been saved but no sales lines were saved - load preview sales lines
            if (_salesLineResults.Results.IsNullOrEmpty())
            {
                await LoadPreviewSalesLines();
            }
        }

        StateHasChanged();
    }

    private bool CanGeneratePreviewSales()
    {
        return !(_activeTruckTicket.FacilityId == Guid.Empty ||
                 _activeTruckTicket.FacilityServiceSubstanceId == null ||
                 _activeTruckTicket.WellClassification == WellClassifications.Undefined ||
                 _activeTruckTicket.BillingCustomerId == Guid.Empty);
    }

    private async Task LoadPreviewSalesLines()
    {
        if (CanGeneratePreviewSales())
        {
            var previewSalesLineRequest = new SalesLinePreviewRequest
            {
                BillingCustomerId = _activeTruckTicket.BillingCustomerId,
                FacilityId = _activeTruckTicket.FacilityId,
                FacilityServiceSubstanceIndexId = _activeTruckTicket.FacilityServiceSubstanceId ?? Guid.Empty,
                WellClassification = _activeTruckTicket.WellClassification,
                LoadDate = _activeTruckTicket.LoadDate,
                SourceLocationId = _activeTruckTicket.SourceLocationId,
                GrossWeight = _activeTruckTicket.GrossWeight,
                TareWeight = _activeTruckTicket.TareWeight,
                TotalVolume = _activeTruckTicket.TotalVolume,
                TotalVolumePercent = _activeTruckTicket.TotalVolumePercent,
                OilVolume = _activeTruckTicket.OilVolume,
                OilVolumePercent = _activeTruckTicket.OilVolumePercent,
                WaterVolume = _activeTruckTicket.WaterVolume,
                WaterVolumePercent = _activeTruckTicket.WaterVolumePercent,
                SolidVolume = _activeTruckTicket.SolidVolume,
                SolidVolumePercent = _activeTruckTicket.SolidVolumePercent,
                MaterialApprovalId = _activeTruckTicket.MaterialApprovalId,
                MaterialApprovalNumber = _activeTruckTicket.MaterialApprovalNumber,
                ServiceTypeId = _activeTruckTicket.ServiceTypeId,
                ServiceTypeName = _activeTruckTicket.ServiceType,
                TruckingCompanyId = _activeTruckTicket.TruckingCompanyId,
                TruckingCompanyName = _activeTruckTicket.TruckingCompanyName,
            };

            _salesLineResults.Results = await SalesLineService.GetPreviewSalesLines(previewSalesLineRequest);
            StateHasChanged();
        }
    }

    private async Task<IEnumerable<SalesLine>> SaveSalesLines()
    {
        if (_activeTruckTicket.Id == Guid.Empty)
        {
            return _salesLineResults.Results;
        }

        foreach (var salesLine in _salesLineResults.Results)
        {
            salesLine.TruckTicketId = _activeTruckTicket.Id;
        }

        var response = await SalesLineService.BulkSaveForTruckTicket(_salesLineResults.Results, _activeTruckTicket.Id);
        if (!response.IsSuccessStatusCode)
        {
            return _salesLineResults.Results;
        }

        _salesLineResults.Results = response.Model;
        return response.Model;
    }

    private void CloneTruckTicket(TruckTicket finalTruckTicket)
    {
        //clone from source ticket
        var sourceTruckTicket = _activeTruckTicketBackup.Clone();

        if (finalTruckTicket == null)
        {
            SetClonedValues(sourceTruckTicket);
            NotificationService.Notify(NotificationSeverity.Success, "Truck Ticket Cloned to New Ticket");
        }
        else
        {
            SetOverrideValues(sourceTruckTicket, finalTruckTicket);
            NotificationService.Notify(NotificationSeverity.Success, $"Stub Ticket {finalTruckTicket.TicketNumber} updated");
        }

        _activeTruckTicket = sourceTruckTicket;
        _activeTruckTicketBackup = _activeTruckTicket.Clone();

        StateHasChanged();
    }

    private void SetClonedValues(TruckTicket sourceTicket)
    {
        sourceTicket.Id = default;
        sourceTicket.TicketNumber = default;

        //Volume and cuts
        sourceTicket.OilVolume = default;
        sourceTicket.OilVolumePercent = default;
        sourceTicket.WaterVolume = default;
        sourceTicket.WaterVolumePercent = default;
        sourceTicket.SolidVolume = default;
        sourceTicket.SolidVolumePercent = default;
        sourceTicket.TotalVolume = default;
        sourceTicket.TotalVolumePercent = default;
        sourceTicket.GrossWeight = default;
        sourceTicket.TareWeight = default;
        sourceTicket.NetWeight = default;

        //attachments
        sourceTicket.Attachments = default;

        //additional services
        sourceTicket.AdditionalServices = default;

        //billing related info
        sourceTicket.BillingContact = default;
        sourceTicket.BillingConfigurationId = default;
        sourceTicket.BillingCustomerId = default;
        sourceTicket.BillingCustomerName = default;
        sourceTicket.IsBillingInfoOverridden = default;

        sourceTicket.Status = TruckTicketStatus.New;
    }

    private void SetOverrideValues(TruckTicket sourceTicket, TruckTicket stubTicket)
    {
        sourceTicket.Id = stubTicket.Id;
        sourceTicket.TicketNumber = stubTicket.TicketNumber;

        //Volume and cuts
        sourceTicket.OilVolume = stubTicket.OilVolume;
        sourceTicket.OilVolumePercent = stubTicket.OilVolumePercent;
        sourceTicket.WaterVolume = stubTicket.WaterVolume;
        sourceTicket.WaterVolumePercent = stubTicket.WaterVolumePercent;
        sourceTicket.SolidVolume = stubTicket.SolidVolume;
        sourceTicket.SolidVolumePercent = stubTicket.SolidVolumePercent;
        sourceTicket.TotalVolume = stubTicket.TotalVolume;
        sourceTicket.TotalVolumePercent = stubTicket.TotalVolumePercent;
        sourceTicket.GrossWeight = stubTicket.GrossWeight;
        sourceTicket.TareWeight = stubTicket.TareWeight;
        sourceTicket.NetWeight = stubTicket.NetWeight;

        //attachments
        sourceTicket.Attachments = stubTicket?.Attachments;

        //additional services
        sourceTicket.AdditionalServices = stubTicket?.AdditionalServices;

        //billing related info
        sourceTicket.BillingContact = stubTicket?.BillingContact;
        sourceTicket.BillingConfigurationId = stubTicket?.BillingConfigurationId;
        sourceTicket.BillingCustomerId = stubTicket.BillingCustomerId;
        sourceTicket.BillingCustomerName = stubTicket.BillingCustomerName;
        sourceTicket.IsBillingInfoOverridden = stubTicket.IsBillingInfoOverridden;

        sourceTicket.Status = TruckTicketStatus.Stub;
    }

    private async Task ShowEditMultipleDialog()
    {
        // read the current state
        var selectedTickets = _truckTicketIndex.GetSelectedTickets();
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
            _truckTicketIndex.ClearSelectedTickets();
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
                truckTicket.SourceLocationFormatted = sourceLocation.FormattedIdentifier;
                truckTicket.GeneratorId = sourceLocation.GeneratorId;
                truckTicket.GeneratorName = sourceLocation.GeneratorName;
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

    private async Task<IEnumerable<TruckTicket>> SplitTruckTicket(IEnumerable<TruckTicket> splitTruckTickets)
    {
        return await TruckTicketService.SplitTruckTickets(splitTruckTickets, _activeTruckTicket.Key);
    }

    private async Task SplitTicket()
    {
        _isSpliting = true;

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

        _isSpliting = false;
        RefreshTruckTicket();
    }

    private async Task<bool> ConfirmCustomerOnTicket(IEnumerable<TruckTicket> splitTruckTicket)
    {
        var isSameCustomer = await TruckTicketService.ConfirmCustomerOnTickets(splitTruckTicket);
        return isSameCustomer;
    }
}
