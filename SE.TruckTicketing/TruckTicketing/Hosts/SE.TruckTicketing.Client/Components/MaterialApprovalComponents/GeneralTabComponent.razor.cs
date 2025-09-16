using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components.Accounts.Edit;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Api.Search;
using Trident.Search;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;
using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class GeneralTabComponent
{
    private ProductDropDown<Guid> _additionalServiceDropDown;

    private List<AccountContact> _billingCustomerContacts;

    private EditContext _editContext;

    private bool _enableOnLegalEntityCA;

    private bool _enableOnLegalEntityChangeCA;

    private Facility _facility;

    private FacilityDropDown<Guid> _facilityDropDown;

    private TridentApiDropDownDataGrid<FacilityServiceSubstanceIndex, Guid?> _facilityServiceDropDown;

    private List<AccountContact> _generatorRepDropDownData;

    private List<AccountContactMap> _signatoryContacts = new();

    private SourceLocationDropDown<Guid> _sourceLocationDropDown;

    private List<AccountContact> _thirdPartyCompanyContacts;

    private List<AccountContact> _truckingCompanyContacts;

    protected RadzenDropDown<Guid?> BillingCustomerContactDropDown;

    protected AccountsDropDown<Guid> BillingCustomerDropDown;

    protected RadzenDropDown<Guid?> ThirdPartyCompanyContactDropDown;

    protected AccountsDropDown<Guid> ThirdPartyCompanyDropDown;

    protected RadzenDropDown<Guid?> TruckingCompanyContactDropDown;

    protected AccountsDropDown<Guid> TruckingCompanyDropDown;

    private CountryCode _legalEntityCountryCode { get; set; }

    [Parameter]
    public MaterialApproval Model { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    public IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    [Parameter]
    public EventCallback<List<AccountContactMap>> AccountContacts { get; set; }

    [Parameter]
    public EventCallback<bool> ResetAccountContacts { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    [Parameter]
    public bool IsEditable { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        _editContext = new(Model);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;

        _enableOnLegalEntityCA = Model.CountryCode == CountryCode.CA;

        await base.OnInitializedAsync();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnContextChange.InvokeAsync(e.FieldIdentifier);
    }

    private async Task OnLegalEntityChange(LegalEntity legalEntity)
    {
        Model.LegalEntityId = legalEntity.Id;
        Model.LegalEntity = legalEntity.Code;
        Model.CountryCode = legalEntity.CountryCode;

        //Cascading Reset
        Model.FacilityId = default;
        Model.FacilityServiceId = default;
        Model.FacilityServiceName = null;
        Model.FacilityServiceNumber = null;
        Model.WasteCodeName = null;
        Model.SubstanceId = default;
        Model.SubstanceName = null;

        _enableOnLegalEntityChangeCA = legalEntity.CountryCode == CountryCode.CA;
        _facility = new();
        //reset SourceLocation
        Model.SourceLocationId = default;
        Model.SourceLocation = null;
        Model.SourceLocationFormattedIdentifier = null;
        Model.SourceLocationUnformattedIdentifier = null;
        Model.GeneratorId = default;
        Model.GeneratorName = null;
        Model.DownHoleType = default;
        Model.GeneratorRepresenativeId = default;
        Model.GeneratorRepresentative = null;
        _generatorRepDropDownData = new();

        //reset customer references
        Model.BillingCustomerId = default;
        Model.BillingCustomerName = null;
        Model.BillingCustomerContactId = default;
        Model.BillingCustomerContact = null;
        Model.BillingCustomerContactAddress = null;
        _billingCustomerContacts = new();

        //reset thirdPartyCompany references
        Model.ThirdPartyAnalyticalCompanyId = default;
        Model.ThirdPartyAnalyticalCompanyName = null;
        Model.ThirdPartyAnalyticalCompanyContactId = default;
        Model.ThirdPartyAnalyticalCompanyContact = null;
        _thirdPartyCompanyContacts = new();

        //reset truckingCompany references
        Model.TruckingCompanyId = default;
        Model.TruckingCompanyName = null;
        Model.TruckingCompanyContactId = default;
        Model.TruckingCompanyContact = null;
        _truckingCompanyContacts = new();

        await _facilityDropDown.Reload();
        await _facilityServiceDropDown.Reload();
        await _sourceLocationDropDown.Reload();
        await TruckingCompanyDropDown.Reload();
        await BillingCustomerDropDown.Reload();
        await ThirdPartyCompanyDropDown.Reload();
        await ResetAccountContacts.InvokeAsync(true);
    }

    private void OnFacilityLoading(SearchCriteriaModel arg)
    {
        arg.Filters[nameof(Facility.LegalEntityId)] = Model.LegalEntityId;
    }

    private void OnFacilityServiceLoading(SearchCriteriaModel arg)
    {
        arg.Filters[nameof(FacilityServiceSubstanceIndex.FacilityId)] = Model.FacilityId;
        arg.Filters[nameof(FacilityServiceSubstanceIndex.IsAuthorized)] = true;
    }

    private void OnGeneratorRepDropdownChange(object account)
    {
        var selectedRep = account as AccountContact ?? _generatorRepDropDownData?.Where(x => x.Id == (Guid)account).FirstOrDefault(new AccountContact());

        Model.GeneratorRepresentative = $"{selectedRep?.Name} {selectedRep?.LastName}";
    }

    private async Task OnSourceLocationChange(SourceLocation sourceLocation)
    {
        Model.SourceLocationId = sourceLocation.Id;
        Model.SourceLocation = sourceLocation.SourceLocationName;
        Model.SourceLocationFormattedIdentifier = sourceLocation.FormattedIdentifier;
        Model.SourceLocationUnformattedIdentifier = sourceLocation.Identifier;
        Model.GeneratorId = sourceLocation.GeneratorId;
        Model.GeneratorName = sourceLocation.GeneratorName;
        Model.DownHoleType = sourceLocation.DownHoleType ?? DownHoleType.Undefined;

        var generatorAccount = await AccountService.GetById(Model.GeneratorId);
        _generatorRepDropDownData = generatorAccount?.Contacts.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.GeneratorRepresentative.ToString())).ToList() ?? new();

        if (generatorAccount is { Contacts: { } } && generatorAccount.Contacts.Any())
        {
            await PublishAccountContacts(generatorAccount?.Contacts ?? new(), Model.GeneratorId, Model.GeneratorName);
        }
    }

    private async Task OnSourceLocationLoad(SourceLocation sourceLocation)
    {
        var generatorAccount = await AccountService.GetById(Model.GeneratorId);
        _generatorRepDropDownData = generatorAccount?.Contacts.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.GeneratorRepresentative.ToString())).ToList() ?? new();

        if (generatorAccount is { Contacts: { } } && generatorAccount.Contacts.Any())
        {
            await PublishAccountContacts(generatorAccount?.Contacts ?? new(), Model.GeneratorId, Model.GeneratorName);
        }
    }

    private async Task OnFacilityChange(Facility arg)
    {
        Model.Facility = arg.Name;
        Model.SiteId = arg.SiteId;
        Model.FacilityServiceId = default;
        Model.FacilityServiceNumber = default;
        Model.FacilityServiceName = default;
        Model.SubstanceId = default;
        Model.SubstanceName = null;
        Model.WasteCodeName = null;
        Model.AdditionalService = default;
        Model.AdditionalServiceName = null;
        Model.ActivateAutofillTareWeight = arg.EnableTareWeight;
        _facility = arg;
        await _facilityServiceDropDown.Reload();
        await _additionalServiceDropDown.Reload();
    }

    private void OnFacilityLoad(Facility facility)
    {
        _facility = facility;
        Model.SiteId = facility.SiteId;
    }

    private void HandleFacilityServiceChange(FacilityServiceSubstanceIndex facilityService)
    {
        Model.FacilityServiceId = facilityService.FacilityServiceId;
        Model.ServiceTypeId = facilityService.ServiceTypeId;
        Model.FacilityServiceNumber = facilityService.FacilityServiceNumber;
        Model.FacilityServiceName = facilityService.ServiceTypeName;
        Model.SubstanceId = facilityService.SubstanceId;
        Model.SubstanceName = facilityService.Substance;
        Model.WasteCodeName = facilityService.WasteCode;
        Model.DisposalUnits = facilityService.UnitOfMeasure;
        Model.Stream = facilityService.Stream;
    }

    private async Task OnCreateNewAccount(string accountType)
    {
        if (accountType == AccountTypes.Customer.ToString())
        {
            await BillingCustomerDropDown.CreateNewAccount();
        }
        else if (accountType == AccountTypes.ThirdPartyAnalytical.ToString())
        {
            await ThirdPartyCompanyDropDown.CreateNewAccount();
        }
        else if (accountType == AccountTypes.TruckingCompany.ToString())
        {
            await TruckingCompanyDropDown.CreateNewAccount();
        }
    }

    private void HandleSourceLocationLoading(SearchCriteriaModel criteria)
    {
        //Apply filter on SourceLocation by LegalEntity CountryCode
        if (Model.CountryCode == default)
        {
            return;
        }

        criteria.Filters[nameof(SourceLocation.CountryCode)] = Model.CountryCode.ToString();
    }

    private void HandleThirdPartyCompanyLoading(SearchCriteriaModel criteria)
    {
        //Apply filter on ThirdPartyCompany dropdown by LegalEntity selected
        if (Model.LegalEntityId == Guid.Empty)
        {
            return;
        }

        criteria.Filters[nameof(Account.LegalEntityId)] = Model.LegalEntityId;
    }

    private void HandleCustomerLoading(SearchCriteriaModel criteria)
    {
        //Apply filter on Customer dropdown by LegalEntity selected
        if (Model.LegalEntityId == Guid.Empty)
        {
            return;
        }

        criteria.Filters[nameof(Account.LegalEntityId)] = Model.LegalEntityId;
    }

    private void HandleTruckingCompanyLoading(SearchCriteriaModel criteria)
    {
        //Apply filter on Customer dropdown by LegalEntity selected
        if (Model.LegalEntityId == Guid.Empty)
        {
            return;
        }

        criteria.Filters[nameof(Account.LegalEntityId)] = Model.LegalEntityId;
    }

    protected void HandleLegalEntityLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(LegalEntity.Name);
        criteria.Filters[nameof(LegalEntity.ShowAccountsInTruckTicketing)] = true;
    }

    private async Task OnBillingCustomerDropdownChange(Account account)
    {
        _billingCustomerContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString()) && !x.IsDeleted).OrderBy(x => x.Name).ToList() ??
                                   new List<AccountContact>();

        Model.BillingCustomerName = account?.Name;
        Model.BillingCustomerContactId = null;

        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), Model.BillingCustomerId, Model.BillingCustomerName);
        }
    }

    private async Task OnBillingCustomerDropdownLoad(Account account)
    {
        _billingCustomerContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString()) && !x.IsDeleted).OrderBy(x => x.Name).ToList() ??
                                   new List<AccountContact>();

        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), Model.BillingCustomerId, Model.BillingCustomerName);
        }
    }

    private async Task OpenContactEditDialog(Guid accountId, string contactFunction, string accountType)
    {
        var account = await AccountService.GetById(accountId);
        if (account == null)
        {
            return;
        }

        var legalEntity = await LegalEntityService.GetById(account.LegalEntityId);
        _legalEntityCountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;

        var _accountContactViewModel = new AccountContactViewModel(new()
        {
            AccountContactAddress = new() { Country = _legalEntityCountryCode },
            ContactFunctions = string.IsNullOrEmpty(contactFunction) ? new() : new() { contactFunction },
            IsDeleted = false,
            IsActive = true,
        }, account);

        await DialogService.OpenAsync<AddEditAccountContact>(_accountContactViewModel.Title,
                                                             new()
                                                             {
                                                                 { nameof(AddEditAccountContact.ViewModel), _accountContactViewModel },
                                                                 {
                                                                     nameof(AddEditAccountContact.OnSubmit), new EventCallback<AccountContact>(this,
                                                                         (Func<AccountContact, Task>)(async model => await UpdateAccountContact(account, model, accountType)))
                                                                 },
                                                                 { nameof(AddEditAccountContact.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                             },
                                                             new()
                                                             {
                                                                 Width = "60%",
                                                             });
    }

    private async Task UpdateAccountContact(Account account, AccountContact contact, string accountType)
    {
        if (contact.Id == default)
        {
            contact.Id = Guid.NewGuid();
        }

        account.Contacts.Add(contact);
        var response = await AccountService.Update(account);
        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Account contact created.");
        }

        if (!contact.IsDeleted)
        {
            if (accountType == AccountTypes.Customer.ToString() && contact.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString()))
            {
                _billingCustomerContacts.Add(contact);
                Model.BillingCustomerContactId = contact.Id;
                OnBillingCustomerContactChange(contact.Id);
            }
            else if (accountType == AccountTypes.ThirdPartyAnalytical.ToString() && contact.ContactFunctions.Contains(AccountContactFunctions.ThirdPartyContact.ToString()))
            {
                _thirdPartyCompanyContacts.Add(contact);
                Model.ThirdPartyAnalyticalCompanyContactId = contact.Id;
                OnThirdPartyContactChange(contact.Id);
            }
            else if (accountType == AccountTypes.TruckingCompany.ToString())
            {
                _truckingCompanyContacts.Add(contact);
                OnTruckingCompanyContactChange(contact.Id);
            }
            else if (accountType == AccountTypes.Generator.ToString())
            {
                _generatorRepDropDownData.Add(contact);
                Model.GeneratorRepresenativeId = contact.Id;
                OnGeneratorRepDropdownChange(contact);
            }
        }

        DialogService.Close();
        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), account.Id, account.Name);
        }

        StateHasChanged();
        await Task.CompletedTask;
    }

    private void OnBillingCustomerContactChange(object value)
    {
        var changeRecord = _billingCustomerContacts?.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        Model.BillingCustomerContact = changeRecord?.DisplayName;
        Model.BillingCustomerContactAddress = changeRecord?.Address;
    }

    private async Task OnThirdPartyDropdownChange(Account account)
    {
        _thirdPartyCompanyContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.ThirdPartyContact.ToString()) && !x.IsDeleted).ToList() ??
                                     new List<AccountContact>();

        Model.ThirdPartyAnalyticalCompanyName = account?.Name;
        Model.ThirdPartyAnalyticalCompanyContactId = null;
        Model.ThirdPartyAnalyticalCompanyContact = null;
        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), Model.ThirdPartyAnalyticalCompanyId, Model.ThirdPartyAnalyticalCompanyName);
        }
    }

    private async Task OnThirdPartyDropdownLoad(Account account)
    {
        _thirdPartyCompanyContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.ThirdPartyContact.ToString()) && !x.IsDeleted).ToList() ??
                                     new List<AccountContact>();

        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), Model.ThirdPartyAnalyticalCompanyId, Model.ThirdPartyAnalyticalCompanyName);
        }
    }

    private void OnThirdPartyContactChange(object value)
    {
        var changeRecord = _thirdPartyCompanyContacts?.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        Model.ThirdPartyAnalyticalCompanyContact = changeRecord?.DisplayName;
    }

    private async Task OnTruckingCompanyDropdownChange(Account account)
    {
        _truckingCompanyContacts = account?.Contacts?.Where(x => !x.IsDeleted).ToList() ?? new List<AccountContact>();

        Model.TruckingCompanyName = account?.Name;
        Model.TruckingCompanyContactId = null;
        Model.TruckingCompanyContact = null;
        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), Model.TruckingCompanyId, Model.TruckingCompanyName);
        }
    }

    private async Task OnTruckingCompanyDropdownLoad(Account account)
    {
        _truckingCompanyContacts = account?.Contacts?.Where(x => !x.IsDeleted).ToList() ?? new List<AccountContact>();
        if (account is { Contacts: { } } && account.Contacts.Any())
        {
            await PublishAccountContacts(account?.Contacts ?? new(), Model.TruckingCompanyId, Model.TruckingCompanyName);
        }
    }

    private void OnTruckingCompanyContactChange(object value)
    {
        var changeRecord = _truckingCompanyContacts?.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        if (changeRecord == null || !changeRecord.Email.HasText())
        {
            Model.TruckingCompanyContactId = default;
            NotificationService.Notify(NotificationSeverity.Error, detail: "Unable to select contact with no Email Address specified.");
            return;
        }

        if (Model.LoadSummaryReportRecipients.Any(x => x.AccountContactId == Model.TruckingCompanyContactId))
        {
            Model.LoadSummaryReportRecipients.Remove(Model.LoadSummaryReportRecipients.First(x => x.AccountContactId == Model.TruckingCompanyContactId));
        }

        Model.TruckingCompanyContactId = changeRecord?.Id;
        Model.TruckingCompanyContact = changeRecord?.Name;
        Model.LoadSummaryReportRecipients.Add(new()
        {
            AccountContactId = changeRecord.Id,
            AccountName = Model?.TruckingCompanyName,
            ReportRecipientName = changeRecord.DisplayName,
            JobTitle = changeRecord.JobTitle,
            ReceiveLoadSummary = true,
            PhoneNumber = changeRecord.PhoneNumber,
            Email = changeRecord.Email,
        });

        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.LoadSummaryReportRecipients)));
    }

    private void OnAdditionalServiceChange(Product product)
    {
        Model.AdditionalServiceName = product.Name;
    }

    private void LoadAdditionalServices(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Product.LegalEntityId)] = Model.LegalEntityId;

        var allowedCategories = new[]
        {
            ProductCategories.AdditionalServices.Liner, ProductCategories.AdditionalServices.AltUnitOfMeasureClass1, ProductCategories.AdditionalServices.AltUnitOfMeasureClass2,
        };

        criteria.Filters[nameof(Product.Categories)] = allowedCategories.AsInclusionAxiomFilter(nameof(Product.Categories).AsPrimitiveCollectionFilterKey());

        if (Model.FacilityId == default || !Model.Facility.HasText())
        {
            return;
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
                               Value = Model.SiteId,
                               Operator = CompareOperators.contains,
                               Key = "AllowedSites2",
                           })
                          .EndGroup()
                          .Build();

        criteria.Filters[nameof(Product.AllowedSites)] = allowedSites;
    }

    #region Signatories

    private async Task PublishAccountContacts(List<AccountContact> accountContacts, Guid? accountId, string accountName)
    {
        if (!accountContacts.Any() || accountContacts.All(x => x.IsDeleted))
        {
            return;
        }

        var contacts = accountContacts.Select(contact => new AccountContactMap
                                       {
                                           AccountId = accountId,
                                           AccountName = accountName,
                                           Contact = contact,
                                       }).Where(x => !x.Contact.IsDeleted && x.Contact.Email.HasText())
                                      .ToList();

        if (accountId != Model.TruckingCompanyId)
        {
            foreach (var contact in contacts.Where(contact => _signatoryContacts.All(x => x.Id != contact.Id) &&
                                                              contact.Contact.ContactFunctions.Contains(AccountContactFunctions.FieldSignatoryContact.ToString())))
            {
                _signatoryContacts.Add(contact);
            }
        }

        await AccountContacts.InvokeAsync(contacts);
    }

    private void UpdateApplicantSignatoryReceiveLoadSummary(ApplicantSignatory applicantSignatory)
    {
        foreach (var signatory in Model.ApplicantSignatories.Where(x => x.AccountContactId == applicantSignatory.AccountContactId))
        {
            signatory.ReceiveLoadSummary = applicantSignatory.ReceiveLoadSummary;
        }

        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.ApplicantSignatories)));
    }

    private void ApplicantSignatoryDeleted(ApplicantSignatory applicantSignatory)
    {
        var deliveryContacts = new List<ApplicantSignatory>(Model.ApplicantSignatories);
        deliveryContacts.Remove(applicantSignatory);
        Model.ApplicantSignatories = deliveryContacts;
        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.ApplicantSignatories)));
    }

    private void AddNewApplicantSignatory(ApplicantSignatory applicantSignatory)
    {
        if (Model.ApplicantSignatories.Count(x => x.ReceiveLoadSummary) >= 2)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Only up to 2 Signatories are allowed to be selected.");
            return;
        }

        var applicantSignatories = new List<ApplicantSignatory>(Model.ApplicantSignatories) { applicantSignatory };

        Model.ApplicantSignatories = applicantSignatories;
        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.ApplicantSignatories)));
    }

    #endregion
    
    private async Task OnCreateSourceLocation()
    {
        await _sourceLocationDropDown.CreateOrUpdateSourceLocation(Model.CountryCode,null);
    }
}
