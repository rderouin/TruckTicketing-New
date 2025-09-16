using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Newtonsoft.Json;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.Accounts.Edit;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;
using SE.TruckTicketing.UI.ViewModels.SourceLocations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Contracts.Configuration;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Pages.SourceLocations;

public partial class SourceLocationDetailsPage : BaseTruckTicketingComponent
{
    private EditContext _editContext;

    private Account _generator;

    private AccountContactViewModel _generatorContactViewModel;

    private SourceLocationDetailsViewModel _viewModel = new(new(), new());

    private AssociatedSourceLocationsIndex associatedSourceLocationIndex;

    protected AccountsDropDown<Guid> ContractOperatorDropDown;

    protected AccountsDropDown<Guid> GeneratorDropDown;

    protected AccountContactDropDown<Guid?> GeneratorPaContactDropDown;

    protected bool IsCloning;

    protected bool IsDeleting;

    protected bool IsSaving;

    protected AccountContactDropDown<Guid?> OperatorPaContactDropDown;

    protected Response<SourceLocation> Response;

    private bool DisableAddGeneratorContact => _viewModel.SourceLocation.GeneratorId == Guid.Empty;

    private bool DisableAddContractOperatorContact => _viewModel.SourceLocation.ContractOperatorId == Guid.Empty;

    private string ReturnUrl => "/truck-ticketing/source-locations";

    [Parameter]
    public Guid? Id { get; set; }
   
    [Parameter]
    public CountryCode LegalEntityCountryCode { get; set; } = default;

    [Parameter]
    public EventCallback<SourceLocation> AddSourceLocation { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> EditSourceLocation { get; set; }

    [Parameter]
    public bool IsEditable { get; set; } = false;

    [Inject]
    private IAppSettings AppSettings { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ISourceLocationService SourceLocationService { get; set; }

    [Inject]
    private IServiceBase<SourceLocationType, Guid> SourceLocationTypeService { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    public IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    private string ThreadId => $"SourceLocation|{Id}";

    [Inject]
    public IServiceProxyBase<Note, Guid> NotesService { get; set; }

    private bool DisableDeleteSourceLocation { get; set; }

    private bool DisableCloneSourceLocation { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;
        if (Id != default)
        {
            var criteria = new SearchCriteriaModel
            {
                Filters = new()
                {
                    { nameof(TruckTicket.SourceLocationId), Id },
                },
                PageSize = 1,
            };

            criteria.Filters[nameof(TruckTicket.Status)] = new CompareModel
            {
                Operator = CompareOperators.ne,
                Value = TruckTicketStatus.Void.ToString(),
            };

            var truckTicketSearchResult = await TruckTicketService.Search(criteria);
            DisableDeleteSourceLocation = truckTicketSearchResult?.Results.Any() == true;

            await LoadSourceLocation(null, Id.Value);
        }
        else
        {
            await LoadSourceLocation(null);
        }

        IsLoading = false;
    }

    private void AssociateSourceLocation(SourceLocation associatedSourceLocation)
    {
        _viewModel.SourceLocation.AssociatedSourceLocationId = associatedSourceLocation?.Id;
        _viewModel.SourceLocation.AssociatedSourceLocationCode = associatedSourceLocation?.SourceLocationCode;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(SourceLocation.AssociatedSourceLocationId)));
    }

    private async Task LoadSourceLocation(SourceLocation model, Guid? id = null)
    {
        var sourceLocationModel = model ?? (id is null ? new() : await SourceLocationService.GetById(id.Value));
        _viewModel = new(sourceLocationModel, AppSettings.GetSection<SourceLocationSettings>(nameof(SourceLocationSettings)));
        _editContext = new(sourceLocationModel);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        StateHasChanged();
    }

    public override void Dispose()
    {
        if (_editContext is not null)
        {
            _editContext.OnFieldChanged -= OnEditContextFieldChanged;
        }
    }

    private void HandleOwnershipDateChange(DateTimeOffset? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        var startDate = value.Value;
        _viewModel.SourceLocation.GeneratorStartDate = new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, 7, 0, 0, new(0)).ToAlbertaOffset();
    }

    private void DateRenderRestrictFutureDate(DateRenderEventArgs args)
    {
        args.Disabled = args.Disabled || args.Date > DateTime.Today;
    }

    private async Task HandleContractOperatorChange(Account account)
    {
        _viewModel.SourceLocation.ContractOperatorProductionAccountContactId = null;
        _viewModel.SourceLocation.ContractOperatorName = account.Name;
        await OperatorPaContactDropDown.Reload(account.Id);

        StateHasChanged();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _viewModel.SubmitButtonDisabled = !_editContext.IsModified();
    }

    protected void HandleMaskedIdentifierChange(object args)
    {
        var formattedIdentifier = args as string ?? String.Empty;
        _viewModel.SetFormattedIdentifier(formattedIdentifier.ToUpper());
    }

    protected async Task HandleGeneratorChange(Account account)
    {
        _generator = account;
        _viewModel.SourceLocation.GeneratorId = account.Id;
        _viewModel.SourceLocation.GeneratorProductionAccountContactId = null;
        _viewModel.SourceLocation.ContractOperatorId = account.Id;
        _viewModel.SourceLocation.ContractOperatorName = account.Name;
        _viewModel.SourceLocation.ContractOperatorProductionAccountContactId = null;

        await ContractOperatorDropDown.Reload();

        if (_viewModel.SourceLocationType?.CountryCode != CountryCode.US)
        {
            await Task.WhenAll(OperatorPaContactDropDown.Reload(account.Id), GeneratorPaContactDropDown.Reload(account.Id));
        }
    }

    private async Task OpenAddContactDialog(Guid accountId, string billingFunction, string origin)
    {
        var account = await AccountService.GetById(accountId);
        if (account == null)
        {
            return;
        }

        var legalEntity = await LegalEntityService.GetById(account.LegalEntityId);
        LegalEntityCountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;

        _generatorContactViewModel = new(new()
        {
            AccountContactAddress = new() { Country = LegalEntityCountryCode },
            ContactFunctions = new() { billingFunction },
            IsDeleted = false,
            IsActive = true,
        }, account);

        await DialogService.OpenAsync<AddEditAccountContact>(_generatorContactViewModel.Title,
                                                             new()
                                                             {
                                                                 { nameof(AddEditAccountContact.ViewModel), _generatorContactViewModel },
                                                                 {
                                                                     nameof(AddEditAccountContact.OnSubmit), new EventCallback<AccountContact>(this,
                                                                         (Func<AccountContact, Task>)(async model => await AddGeneratorContact(model, account, origin)))
                                                                 },
                                                                 { nameof(AddEditAccountContact.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                             },
                                                             new()
                                                             {
                                                                 Width = "60%",
                                                             });
    }

    private async Task AddGeneratorContact(AccountContact contact, Account account, string message)
    {
        account.Contacts.Add(contact);
        var response = await AccountService.Update(account);
        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: $"{message} contact created.");
        }

        DialogService.Close();
        if (_viewModel.SourceLocation.ContractOperatorId != default && _viewModel.SourceLocation.GeneratorId != default &&
            _viewModel.SourceLocation.ContractOperatorId == _viewModel.SourceLocation.GeneratorId)
        {
            await Task.WhenAll(OperatorPaContactDropDown.Reload(_viewModel.SourceLocation.ContractOperatorId), GeneratorPaContactDropDown.Reload(_viewModel.SourceLocation.GeneratorId));
            StateHasChanged();
            return;
        }

        switch (message)
        {
            case "Generator":
                await GeneratorPaContactDropDown.Reload(_viewModel.SourceLocation.GeneratorId);
                break;
            case "Contract Operator":
                await OperatorPaContactDropDown.Reload(_viewModel.SourceLocation.ContractOperatorId);
                break;
        }

        StateHasChanged();
    }

    protected async Task HandleGeneratorProductionAccountantChange(AccountContactIndex contact)
    {
        var sourceLocation = _viewModel.SourceLocation;
        sourceLocation.GeneratorProductionAccountContactId = contact?.Id;

        if (sourceLocation.ContractOperatorId == sourceLocation.GeneratorId)
        {
            _viewModel.SourceLocation.ContractOperatorProductionAccountContactId = contact?.Id;
            await OperatorPaContactDropDown.Reload();
        }
    }

    protected async Task OnHandleSubmit()
    {
        IsSaving = true;

        if (_viewModel.SourceLocationType == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Required to select Source Location Type.");
            IsSaving = false;
            return;
        }

        if (_viewModel.SourceLocationType.CountryCode == CountryCode.US)
        {
            var criteria = new SearchCriteriaModel
            {
                Filters = new()
                {
                    [nameof(SourceLocation.IsDeleted)] = false,
                    [nameof(SourceLocation.CountryCode)] = CountryCode.US.ToString(),
                    [nameof(SourceLocation.SourceLocationName)] = _viewModel.SourceLocation.SourceLocationName,
                    [nameof(SourceLocation.SourceLocationTypeId)] = new CompareModel
                    {
                        Value = _viewModel.SourceLocation.SourceLocationTypeId,
                        Operator = CompareOperators.ne,
                    },
                },
                PageSize = 10,
            };

            var results = await SourceLocationService.Search(criteria);
            var similarSourceLocationTypes = string.Join(", ", results?.Results.Select(sl => sl.SourceLocationTypeName) ?? Enumerable.Empty<string>());
            if (similarSourceLocationTypes.HasText())
            {
                var allow = await
                                DialogService
                                   .Confirm($"There are one or more source locations of type [{similarSourceLocationTypes}] with the same name: '{_viewModel.SourceLocation.SourceLocationName}'. Please confirm this is intended.",
                                            "Similar US Source Location Check",
                                            new()
                                            {
                                                CancelButtonText = "Cancel",
                                                OkButtonText = "Proceed",
                                            }) ?? true;

                if (!allow)
                {
                    IsSaving = false;
                    return;
                }
            }
        }

        _viewModel.SourceLocation.ProvinceOrStateString = _viewModel.SourceLocation.ProvinceOrState.GetDescription();
        var response = _viewModel.IsNew ? await SourceLocationService.Create(_viewModel.SourceLocation) : await SourceLocationService.Update(_viewModel.SourceLocation);

        IsSaving = false;

        if (response.IsSuccessStatusCode)
        {
            if (IsEditable)
            {
                if(Id != default)
                {
                    await EditSourceLocation.InvokeAsync(response.Model);
                }
                else
                {
                    await AddSourceLocation.InvokeAsync(response.Model);
                }
                DialogService.Close();
            }

            if (!IsEditable)
            {
                NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
                await LoadSourceLocation(response.Model);
            }
        }

        Response = response;
    }

    protected async Task DeleteSourceLocation()
    {
        IsDeleting = true;

        var response = await SourceLocationService.MarkSourceLocationDeleted(_viewModel.SourceLocation.Id);
        var isSourceLocationDeleted = JsonConvert.DeserializeObject<bool>(response.ResponseContent);
        IsDeleting = false;

        if (response.IsSuccessStatusCode && isSourceLocationDeleted)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Source location deleted.");
            NavigationManager.NavigateTo("/truck-ticketing/source-locations");
        }
        else if (response.IsSuccessStatusCode && !isSourceLocationDeleted)
        {
            NotificationService.Notify(NotificationSeverity.Info, detail: "Source location has active ticket and can not be deleted");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Something went wrong while trying to delete the source location");
        }
    }

    protected void CloneSourceLocation()
    {
        IsCloning = true;
        var destSourceLocation = _viewModel.SourceLocation.Clone();
        destSourceLocation.SourceLocationVerified = false;

        NotificationService.Notify(NotificationSeverity.Success, $"Source Location {destSourceLocation.SourceLocationName} updated");

        ExcludeSelectedValues(destSourceLocation);
        _viewModel = new(destSourceLocation, AppSettings.GetSection<SourceLocationSettings>(nameof(SourceLocationSettings)));
        _editContext = new(destSourceLocation);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        IsCloning = false;
    }

    private void ExcludeSelectedValues(SourceLocation destSourceLocation)
    {
        destSourceLocation.Id = default;
        destSourceLocation.ApiNumber = default;
        destSourceLocation.WellFileNumber = default;
    }

    protected void OnSourceLocationDissociate(Guid associatedSourceLocationId)
    {
        if (associatedSourceLocationId == _viewModel.SourceLocation.AssociatedSourceLocationId)
        {
            AssociateSourceLocation(null);
        }
    }
    private void HandleSourceLocationTypeLoading(SearchCriteriaModel criteria)
    {
        if (LegalEntityCountryCode != default) { 
            criteria.Filters[nameof(SourceLocationType.CountryCode)] = LegalEntityCountryCode.ToString();
        }
        criteria.Filters[nameof(SourceLocationType.IsActive)] = true;

    }
    protected void OnSourceLocationTypeLoad(SourceLocationType sourceLocationType)
    {
        _viewModel.UpdateIdentifierMask(sourceLocationType);
        _viewModel.UpdateSourceLocationCodeMask(sourceLocationType);
        _viewModel.SourceLocationType = sourceLocationType;
    }

    protected async Task OnCreateNewAccount()
    {
        await GeneratorDropDown.CreateNewAccount();
    }

    protected string GetRegistryUrls(string registryName)
    {
        return registryName switch
               {
                   "MBOGC" => PetroleumRegistryLinks.MBOGC,
                   "NDIC" => PetroleumRegistryLinks.NDIC,
                   "IRIS" => PetroleumRegistryLinks.IRIS,
                   "BCOGC" => PetroleumRegistryLinks.BCOGC,
                   "PETRINEX" => PetroleumRegistryLinks.PETRINEX,
                   _ => String.Empty,
               };
    }

    private async Task<SearchResultsModel<Note, SearchCriteriaModel>> LoadNotes(SearchCriteriaModel criteria)
    {
        return await NotesService.Search(criteria);
    }

    private async Task<bool> HandleNoteUpdate(Note note)
    {
        var response = note.Id == Guid.Empty ? await NotesService.Create(note) : await NotesService.Update(note);
        return response.IsSuccessStatusCode;
    }

    private bool HasGeneratorAccess
    {
        get
        {
            return (
                     _viewModel.IsNew ||
                     IsAuthorizedFor(Permissions.Resources.SourceLocationOwnershipInfo, Permissions.Operations.Write)
                   );
        }
    }

    private bool HasSourceLocationNameIdWriteAccess
    {
        get
        {
            return IsAuthorizedFor(Permissions.Resources.SourceLocationNameId, Permissions.Operations.Write);
        }
    }

    private void CloseButton_Click()
    {
        if (IsEditable)
        {
            DialogService.Close();
        }
        else
        {
            NavigationManager.NavigateTo(ReturnUrl);
        }
    }
}
