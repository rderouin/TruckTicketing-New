using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Newtonsoft.Json;

using Radzen;
using Radzen.Blazor;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Search;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;
using CompareOperators = Trident.Search.CompareOperators;
using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages.BillingConfig;

public partial class BillingConfigurationEdit : BaseTruckTicketingComponent
{
    private readonly SearchResultsModel<InvoiceExchangeDto, SearchCriteriaModel> _platformCodeResults = new()
    {
        Info = new()
        {
            OrderBy = nameof(InvoiceExchangeDto.PlatformCode),
            SortOrder = SortOrder.Asc,
            Filters = new()
            {
                [nameof(InvoiceExchangeDto.IsDeleted)] = false,
                [nameof(InvoiceExchangeDto.Type)] = InvoiceExchangeType.Global.ToString(),
            },
        },
    };

    private readonly Dictionary<Guid, List<SourceLocation>> GeneratorSourceLocationMap = new();

    private IEnumerable<Guid> _facilities;

    private List<string> _platformCodes = new();

    private List<SourceLocation> _sourceLocations = new();

    private SearchResultsModel<Facility, SearchCriteriaModel> Results = new();

    private string ReturnUrl => NavigationHistoryManager.GetReturnUrl();

    private bool DisableSaveButton =>
        (_viewModel.BillingConfiguration.IsDefaultConfiguration && !HasWritePermission(Permissions.Resources.DefaultBillingConfig))
     || !HasWritePermission(Permissions.Resources.Account) || IsDisableSaveAndClose;

    private bool _matchCriteriaGridDisabled =>
        _viewModel.BillingConfiguration.IsDefaultConfiguration ||
        !_viewModel.BillingConfiguration.IncludeForAutomation;

    [Parameter]
    public Guid? Id { get; set; }

    [Parameter]
    public string BillingCustomerId { get; set; }

    [Parameter]
    public Guid? InvoiceConfigurationId { get; set; }

    [Inject]
    private IBillingConfigurationService BillingConfigurationService { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private IServiceBase<InvoiceConfiguration, Guid> InvoiceConfigurationService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IEDIFieldDefinitionService EDIFieldDefinitionService { get; set; }

    [Inject]
    public IServiceBase<Note, Guid> NotesService { get; set; }

    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private IServiceBase<InvoiceExchangeDto, Guid> InvoiceExchangeService { get; set; }

    [Inject]
    private IServiceBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Parameter]
    public EventCallback<BillingConfiguration> AddEditBillingConfiguration { get; set; }

    [Parameter]
    public EventCallback<bool> CancelAddEditBillingConfiguration { get; set; }

    [Parameter]
    public string Operation { get; set; }

    [Parameter]
    public bool HideReturnToAccount { get; set; }

    [Parameter]
    public bool IsInvoiceConfigurationCloned { get; set; }

    [Parameter]
    public bool IsDisableSaveAndClose { get; set; }

    [Parameter]
    public BillingConfiguration BillingConfigurationModel { get; set; }

    [Parameter]
    public InvoiceConfiguration InvoiceConfigurationModel { get; set; }

    private IEnumerable<Guid> SelectedFacilities
    {
        get => _facilities ?? _viewModel.BillingConfiguration.Facilities;
        set
        {
            _viewModel.BillingConfiguration.Facilities = new(value ?? Array.Empty<Guid>());
            _facilities = value;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        NotesThreadId = $"BillingConfiguration|{Id}";
        _generatorContacts = new();
        _billingCustomerContacts = new();
        _emailDeliveryContacts = new();
        if (Id != default)
        {
            await LoadBillingConfigurationAsync(BillingConfigurationModel, Id);
        }
        else
        {
            await LoadBillingConfigurationAsync(new());
        }

        await LoadPlatformCodes();
        var billingCustomerId = Guid.TryParse(BillingCustomerId, out var id) ? id : _viewModel.BillingConfiguration.BillingCustomerAccountId;
        if (billingCustomerId != Guid.Empty)
        {
            await Task.WhenAll(LoadBillingCustomerContactAsync(billingCustomerId), LoadEDIFieldDefinitionAsync(billingCustomerId, Id));
            if (_billingCustomer.AccountTypes.Any() && _billingCustomer.AccountTypes.Contains(AccountTypes.Generator.ToString()) && _viewModel.BillingConfiguration.CustomerGeneratorId == Guid.Empty)
            {
                _viewModel.BillingConfiguration.CustomerGeneratorId = _billingCustomer.Id;
                _viewModel.BillingConfiguration.CustomerGeneratorName = _billingCustomer.Name;
                await OnGeneratorDropdownLoad(_billingCustomer);
            }
        }

        if ((InvoiceConfigurationId != Guid.Empty && InvoiceConfigurationId != null) ||
            _viewModel?.BillingConfiguration?.InvoiceConfigurationId != Guid.Empty)
        {
            var invoiceConfigurationId = InvoiceConfigurationId != null && InvoiceConfigurationId != Guid.Empty ? InvoiceConfigurationId : _viewModel?.BillingConfiguration?.InvoiceConfigurationId;
            await LoadInvoiceConfiguration(InvoiceConfigurationModel, invoiceConfigurationId);
        }
        else
        {
            await LoadInvoiceConfiguration(InvoiceConfigurationModel);
        }

        await LoadFacilityData(_billingCustomer.LegalEntityId);

        if (!_viewModel.BillingConfiguration.InvoiceExchange.HasText())
        {
            _viewModel.BillingConfiguration.InvoiceExchange = _invoiceConfiguration?.InvoiceExchange;
        }

        _isLoading = false;

        await base.OnInitializedAsync();
    }

    private async Task LoadPlatformCodes()
    {
        var results = await InvoiceExchangeService.Search(_platformCodeResults.Info);
        _platformCodes = results.Results.Select(ie => ie.PlatformCode).ToList();
    }

    private async Task OnHandleSubmit()
    {
        _isSaving = true;
        _viewModel.BillingConfiguration.BillingCustomerAccountId = Guid.TryParse(BillingCustomerId, out var id) ? id : _invoiceConfiguration.CustomerId;
        _viewModel.BillingConfiguration.BillingCustomerName = _invoiceConfiguration.CustomerName;

        if (_viewModel.BillingConfiguration.Facilities is { Count: 0 })
        {
            _viewModel.BillingConfiguration.Facilities = null;
        }

        var billingConfigurationResponse = _viewModel.IsNew
                                               ? await BillingConfigurationService.Create(_viewModel.BillingConfiguration)
                                               : await BillingConfigurationService.Update(_viewModel.BillingConfiguration);

        if (billingConfigurationResponse.IsSuccessStatusCode && _isNewCommentAdded)
        {
            var billingConfigurationResponseContent = JsonConvert.DeserializeObject<BillingConfiguration>(billingConfigurationResponse.ResponseContent);
            await HandleNoteUpdate(new()
            {
                ThreadId = $"BillingConfiguration|{billingConfigurationResponseContent!.Id}",
                Comment = _viewModel.LastComment,
            });
        }

        _isSaving = false;

        if (billingConfigurationResponse.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
            if (AddEditBillingConfiguration.HasDelegate)
            {
                await AddEditBillingConfiguration.InvokeAsync(_viewModel.BillingConfiguration);
            }
            else
            {
                NavigationManager.NavigateTo(ReturnUrl);
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: _viewModel.SubmitFailNotificationMessage);
        }

        _response = billingConfigurationResponse;
    }

    private void OnDateChange()
    {
        _viewModel.LoadMatchCriteria();
    }

    private async Task LoadMatchPredicatesForUniquenessCheck()
    {
        _isLoadingMatchPredicates = true;
        var searchCriteria = new SearchCriteriaModel
        {
            Filters = new()
            {
                { nameof(BillingConfiguration.CustomerGeneratorId), _viewModel.BillingConfiguration.CustomerGeneratorId },
                { nameof(BillingConfiguration.IncludeForAutomation), true },
            },
        };

        if (_viewModel.BillingConfiguration.Facilities != null && _viewModel.BillingConfiguration.Facilities.Any())
        {
            var keyIdx = 0;
            IJunction query = AxiomFilterBuilder.CreateFilter()
                                                .StartGroup();

            foreach (var v in _viewModel.BillingConfiguration.Facilities)
            {
                if (query is GroupStart groupStart)
                {
                    query = groupStart.AddAxiom(new()
                    {
                        Key = $"facility{keyIdx++}",
                        Field = nameof(BillingConfiguration.Facilities).AsPrimitiveCollectionFilterKey(),
                        Operator = CompareOperators.contains,
                        Value = v,
                    });
                }
                else if (query is AxiomTokenizer axiom)
                {
                    query = axiom.Or().AddAxiom(new()
                    {
                        Key = $"facility{keyIdx++}",
                        Field = nameof(BillingConfiguration.Facilities).AsPrimitiveCollectionFilterKey(),
                        Operator = CompareOperators.contains,
                        Value = v,
                    });
                }

                searchCriteria.Filters[nameof(BillingConfiguration.Facilities).AsPrimitiveCollectionFilterKey()!] = (query as AxiomTokenizer)?.EndGroup().Build();
            }
        }

        var result = await BillingConfigurationService!.Search(searchCriteria)!;
        if (result == null || !result.Results.Any())
        {
            _viewModel.BillingConfigurationsForMatchPredicate = new();
            _viewModel.overlappingMatchPredicates = new();
        }
        else
        {
            _viewModel.BillingConfigurationsForMatchPredicate = new(result.Results);
            _viewModel.LoadMatchCriteria();
        }

        _isLoadingMatchPredicates = false;
    }

    private async Task LoadFacilityData(Guid legalEntityId)
    {
        if (_invoiceConfiguration.Id != default && !_invoiceConfiguration.AllFacilities && _invoiceConfiguration.Facilities.Any())
        {
            //User facilities selected in invoice configuration to load this dropdown
            foreach (var facilityGuid in _invoiceConfiguration.Facilities)
            {
                var facility = await FacilityService!.GetById(facilityGuid);
                if (facility.LegalEntityId == legalEntityId)
                {
                    _viewModel.facilityData.Add(facility);
                }
            }
        }
        else
        {
            var args = new LoadDataArgs();
            var searchCriteriaModel = args.ToSearchCriteriaModel();
            BeforeFacilityLoad(searchCriteriaModel);
            searchCriteriaModel.Filters[nameof(Facility.LegalEntityId)] = legalEntityId;
            Results = await FacilityService!.Search(searchCriteriaModel)!;

            _viewModel.facilityData = Results == null ? new() : Results.Results.ToList();
        }
    }

    private void BeforeFacilityLoad(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(Facility.SiteId);
        criteria.Filters[nameof(Facility.IsActive)] = true;
        criteria.PageSize = Int32.MaxValue;
    }

    private async Task OnFacilitiesChange()
    {
        await LoadMatchPredicatesForUniquenessCheck();
    }

    private async Task OnFacilityChange(object args)
    {
        await LoadMatchPredicatesForUniquenessCheck();
    }

    private async Task OnClickCancel()
    {
        if (CancelAddEditBillingConfiguration.HasDelegate)
        {
            await CancelAddEditBillingConfiguration.InvokeAsync(true);
        }
        else
        {
            NavigationManager.NavigateTo(ReturnUrl);
        }
    }

    private void OnClickReturnToEditAccount()
    {
        NavigationManager.NavigateTo(ReturnUrl);
    }

    private void HandleGeneratorLoading(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Account.LegalEntityId)] = _billingCustomer.LegalEntityId;
    }

    #region Match Criteria

    //Match Criteria
    private void AddEditMatchPredicate(MatchPredicate newMatchCriteria)
    {
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.MatchCriteria)));
    }

    #endregion

    private void HandleFieldTicketDeliveryMethod(FieldTicketDeliveryMethod? method)
    {
        if (method is FieldTicketDeliveryMethod.TicketByTicket)
        {
            _viewModel.BillingConfiguration.IsSignatureRequired = false;
            _viewModel.BillingConfiguration.LoadConfirmationFrequency = LoadConfirmationFrequency.TicketByTicket;
        }
    }

    #region Variables

    private bool _isLoadingMatchPredicates;

    private EditContext _editContext;

    private bool _isLoading;

    private bool _isSaving;

    private Response<BillingConfiguration> _response;

    private BillingConfigurationDetailsViewModel _viewModel = new(new(), null);

    private readonly SearchResultsModel<EDIFieldDefinition, SearchCriteriaModel> _loadEDIFieldDefinition = new();

    private List<AccountContact> _generatorContacts;

    private Account _generatorAccount;

    private List<AccountContact> _billingCustomerContacts;

    private List<AccountContact> _emailDeliveryContacts;

    private Account _billingCustomer;

    private InvoiceConfiguration _invoiceConfiguration;

    private readonly List<EDIFieldValue> _ediFieldValuesToDelete = new();

    protected bool enableSave = false;

    private string NotesThreadId;

    private string CurrentUserName => "Panth";

    private string _replicatedValue;

    private RadzenTextArea _noteEditor;

    private bool _isNewCommentAdded;

    private EditForm _editForm;

    #endregion

    #region Generation Information

    //Load Data
    private async Task LoadBillingConfigurationAsync(BillingConfiguration model, Guid? billingConfigurationId = null)
    {
        var billingConfigurationResult = billingConfigurationId is null
                                             ? new() { InvoiceConfigurationId = InvoiceConfigurationId ?? default }
                                             : Operation == "clone" || model != null
                                                 ? model
                                                 : await BillingConfigurationService.GetById(billingConfigurationId.Value);

        _viewModel = new(billingConfigurationResult ?? new(), Operation);

        _editContext = new(billingConfigurationResult ?? new());
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
    }

    private async Task LoadBillingCustomerContactAsync(Guid id)
    {
        _billingCustomer = await AccountService?.GetById(id)!;
        _billingCustomerContacts =
            _billingCustomer?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString()) && !x.IsDeleted).OrderBy(x => x.Name).ToList() ??
            new List<AccountContact>();

        _emailDeliveryContacts = _billingCustomer?.Contacts?.Where(x => x.Email.HasText() && !x.IsDeleted).OrderBy(x => x.Name).ToList() ??
                                 new List<AccountContact>();
    }

    private async Task LoadInvoiceConfiguration(InvoiceConfiguration model, Guid? loadConfigurationId = null)
    {
        _invoiceConfiguration = loadConfigurationId is null ? new() : Operation == "clone" ? model : await InvoiceConfigurationService.GetById(loadConfigurationId.Value);
        await LoadSourceLocationGeneratorMap();
    }

    private async Task LoadSourceLocationGeneratorMap()
    {
        List<SourceLocation> selectedSourceLocations = new();
        if (_invoiceConfiguration.Id != Guid.Empty && (!_invoiceConfiguration.AllSourceLocations ||
                                                       (_invoiceConfiguration.SourceLocations != null && _invoiceConfiguration.SourceLocations.Any())))
        {
            foreach (var selectedSourceLocation in _invoiceConfiguration.SourceLocations)
            {
                var sourceLocation = await SourceLocationService!.GetById(selectedSourceLocation);
                if (sourceLocation != null)
                {
                    selectedSourceLocations.Add(sourceLocation);
                }
            }

            if (selectedSourceLocations.Any())
            {
                foreach (var sourceLocation in selectedSourceLocations)
                {
                    GeneratorSourceLocationMap.TryAdd(sourceLocation.GeneratorId, new());
                    GeneratorSourceLocationMap[sourceLocation.GeneratorId].Add(sourceLocation);
                }
            }
        }
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        var ediValues = ((BillingConfiguration)_editContext.Model).EDIValueData;
        foreach (var ediValue in ediValues)
        {
            if (!ediValue.EDIFieldValueContent.HasText() && ediValue.DefaultValue != null)
            {
                ediValue.EDIFieldValueContent = ediValue.DefaultValue;
            }
        }

        _viewModel.SubmitButtonDisabled = !_editContext.IsModified();
    }

    private async Task OnGeneratorDropdownChange(Account account)
    {
        _generatorAccount = account;
        _generatorContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.GeneratorRepresentative.ToString()) && !x.IsDeleted).OrderBy(x => x.Name).ToList() ??
                             new List<AccountContact>();

        _viewModel.BillingConfiguration.CustomerGeneratorName = account?.Name;
        _viewModel.BillingConfiguration.GeneratorRepresentativeId = null;
        LoadSourceLocationForGenerator(account);

        await LoadMatchPredicatesForUniquenessCheck();
    }

    private async Task OnGeneratorDropdownLoad(Account account)
    {
        _generatorAccount = account;
        _generatorContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.GeneratorRepresentative.ToString()) && !x.IsDeleted).OrderBy(x => x.Name).ToList() ??
                             new List<AccountContact>();

        LoadSourceLocationForGenerator(account);

        await LoadMatchPredicatesForUniquenessCheck();
    }

    private void LoadSourceLocationForGenerator(Account account)
    {
        if (account != null && GeneratorSourceLocationMap != null && GeneratorSourceLocationMap.Any() && GeneratorSourceLocationMap.ContainsKey(account.Id))
        {
            _sourceLocations = new(GeneratorSourceLocationMap[account.Id]);
        }
    }

    private void OnBillingContactChange(object value)
    {
        var changeRecord = _billingCustomerContacts?.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        _viewModel.BillingConfiguration.BillingContactAddress = changeRecord?.Address;
        _viewModel.BillingConfiguration.BillingContactName = $"{changeRecord?.Name} {changeRecord?.LastName}";
    }

    #endregion

    #region EDI Field Values

    //EDIValues
    private async Task LoadEDIFieldDefinitionAsync(Guid billingCustomerId, Guid? billingConfigId)
    {
        var current = new SearchCriteriaModel
        {
            PageSize = 100,
            CurrentPage = 0,
            Keywords = "",
            Filters = new(),
        };

        current.Filters.TryAdd(nameof(EDIFieldDefinition.CustomerId), billingCustomerId);
        var results = await EDIFieldDefinitionService?.Search(current);

        _loadEDIFieldDefinition.Results = results?.Results ?? new List<EDIFieldDefinition>();
        if (_loadEDIFieldDefinition.Results.Any())
        {
            foreach (var ediFieldDefinitionForCurrentCustomer in _loadEDIFieldDefinition.Results)
            {
                ediFieldDefinitionForCurrentCustomer.IsNew = billingConfigId == null;
                if (!_viewModel.BillingConfiguration.EDIValueData.Any(x => x.EDIFieldDefinitionId == ediFieldDefinitionForCurrentCustomer.Id))
                {
                    _viewModel.BillingConfiguration.EDIValueData.Add(new()
                    {
                        EDIFieldDefinitionId = ediFieldDefinitionForCurrentCustomer.Id,
                        EDIFieldName = ediFieldDefinitionForCurrentCustomer.EDIFieldName,
                        IsNew = billingConfigId == null,
                        DefaultValue = ediFieldDefinitionForCurrentCustomer.DefaultValue,
                    });
                }
            }
        }

        foreach (var ediFieldDefinitionForCurrentCustomer in _viewModel.BillingConfiguration.EDIValueData)
        {
            if (!_loadEDIFieldDefinition.Results.Any(x => x.Id == ediFieldDefinitionForCurrentCustomer.EDIFieldDefinitionId))
            {
                _ediFieldValuesToDelete.Add(ediFieldDefinitionForCurrentCustomer);
            }
        }

        foreach (var removeEdiFieldValues in _ediFieldValuesToDelete)
        {
            _viewModel.BillingConfiguration.EDIValueData.Remove(removeEdiFieldValues);
        }

        _loadEDIFieldDefinition.Info = results.Info;
    }

    private void NewEDIValueReceived(List<EDIValueViewModel> EDIFieldValues)
    {
        var EDIValues = new List<EDIFieldValue>(_viewModel.BillingConfiguration.EDIValueData);
        foreach (var EDIFieldValue in EDIFieldValues)
        {
            if (EDIValues.Exists(x => x.EDIFieldDefinitionId == EDIFieldValue.EDIFieldDefinitionId))
            {
                foreach (var ediValue in EDIValues.Where(x => x.EDIFieldDefinitionId == EDIFieldValue.EDIFieldDefinitionId))
                {
                    ediValue.DefaultValue = EDIFieldValue.DefaultValue;
                    ediValue.EDIFieldValueContent = EDIFieldValue.EDIFieldValueContent;
                    ediValue.IsNew = false;
                }
            }
            else
            {
                EDIValues.Add(new()
                {
                    EDIFieldDefinitionId = EDIFieldValue.EDIFieldDefinitionId,
                    EDIFieldName = EDIFieldValue.EDIFieldName,
                    DefaultValue = EDIFieldValue.DefaultValue,
                    EDIFieldValueContent = EDIFieldValue.EDIFieldValueContent,
                });
            }
        }

        _viewModel.BillingConfiguration.EDIValueData = EDIValues;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.EDIValueData)));
    }

    #endregion

    #region Comments Thread

    //Notes

    private async Task<bool> HandleNoteUpdate(Note note)
    {
        var response = note.Id == default ? await NotesService.Create(note) : await NotesService.Update(note);
        return response.IsSuccessStatusCode;
    }

    private async Task<SearchResultsModel<Note, SearchCriteriaModel>> OnDataLoad(SearchCriteriaModel criteria)
    {
        return await NotesService.Search(criteria);
    }

    private void OnInput(ChangeEventArgs args)
    {
        _isNewCommentAdded = true;
        _replicatedValue = args?.Value?.ToString();
        _viewModel.BillingConfiguration.LastComment = _replicatedValue;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfigurationDetailsViewModel.LastComment)));
    }

    #endregion

    #region Email Delivery Contact

    //Email Delivery Contact
    private void IsEmailDeliveryContactsEnabled(bool isEmailDeliveryContactEnabled)
    {
        _viewModel.BillingConfiguration.EmailDeliveryEnabled = isEmailDeliveryContactEnabled;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.EmailDeliveryEnabled)));
    }

    private Task AddNewEmailDelivery(EmailDeliveryContact emailDeliveryContact)
    {
        var deliveryContacts = new List<EmailDeliveryContact>(_viewModel.BillingConfiguration.EmailDeliveryContacts);
        if (deliveryContacts.Exists(x => x.AccountContactId == emailDeliveryContact.AccountContactId))
        {
            foreach (var address in deliveryContacts.Where(x => x.AccountContactId == emailDeliveryContact.AccountContactId))
            {
                address.IsAuthorized = emailDeliveryContact.IsAuthorized;
            }
        }
        else
        {
            deliveryContacts.Add(emailDeliveryContact);
        }

        _viewModel.BillingConfiguration.EmailDeliveryContacts = deliveryContacts;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.EmailDeliveryContacts)));
        return Task.CompletedTask;
    }

    private void UpdateEmailDeliveryContactDeleted(EmailDeliveryContact emailDeliveryContact)
    {
        var deliveryContacts = new List<EmailDeliveryContact>(_viewModel.BillingConfiguration.EmailDeliveryContacts);
        deliveryContacts.Remove(emailDeliveryContact);
        _viewModel.BillingConfiguration.EmailDeliveryContacts = deliveryContacts;

        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.EmailDeliveryContacts)));
    }

    private void UpdateEmailDeliveryContactAuthorized(EmailDeliveryContact emailDeliveryContact)
    {
        _viewModel.BillingConfiguration.EmailDeliveryContacts.Where(x => x.AccountContactId == emailDeliveryContact.AccountContactId).Select(c =>
        {
            c.IsAuthorized = emailDeliveryContact.IsAuthorized;
            return c;
        }).ToList();

        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.EmailDeliveryContacts)));
    }

    #endregion

    #region Applicant Signatories

    private async Task ApplicantSignatoryAddUpdateHandler(SignatoryContact applicantSignatory)
    {
        var existingApplicantSignatories = new List<SignatoryContact>(_viewModel.BillingConfiguration.Signatories);
        existingApplicantSignatories.Add(applicantSignatory);

        _viewModel.BillingConfiguration.Signatories = existingApplicantSignatories;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.Signatories)));
        await Task.CompletedTask;
    }

    private async Task ApplicantSignatoryDeletedHandler(SignatoryContact applicantSignatory)
    {
        var existingApplicantSignatories = new List<SignatoryContact>(_viewModel.BillingConfiguration.Signatories);
        existingApplicantSignatories.Remove(applicantSignatory);
        _viewModel.BillingConfiguration.Signatories = existingApplicantSignatories;

        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.Signatories)));
        await Task.CompletedTask;
    }

    private async Task ApplicantSignatoryAuthorizeChangeHandler(SignatoryContact applicantSignatory)
    {
        var existingApplicantSignatories = new List<SignatoryContact>(_viewModel.BillingConfiguration.Signatories);
        existingApplicantSignatories.First(x => x.Id == applicantSignatory.Id).IsAuthorized
            = applicantSignatory.IsAuthorized;

        _viewModel.BillingConfiguration.Signatories = existingApplicantSignatories;

        _editContext.NotifyFieldChanged(_editContext.Field(nameof(BillingConfiguration.Signatories)));
        await Task.CompletedTask;
    }

    #endregion

    private Task HandleSubmit()
    {
        return _editForm.EditContext?.Validate() == true ? OnHandleSubmit() : Task.CompletedTask;
    }
}
