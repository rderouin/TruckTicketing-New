using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components.Accounts.Edit;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Api.Search;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Components.BillingConfigurationComponents;

public partial class BillingApplicantSignatoryGrid
{
    private List<AccountContact> _accountContacts;

    private Dictionary<Guid, Guid> _accountContactToAccountMap;

    private List<Account> _accounts;

    private AccountContactViewModel _contactViewModel;

    private bool _isAccountCustomerAndGenerator;

    private SearchResultsModel<SignatoryContact, SearchCriteriaModel> _signatoryContacts = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<SignatoryContact>(),
    };

    private List<AccountContact> _validBillingCustomerContacts => BillingCustomerAccount.Contacts.Where(x => !x.IsDeleted).ToList();

    private List<AccountContact> _validGeneratorContacts => GeneratorAccount.Contacts.Where(x => !x.IsDeleted).ToList();

    private int? _pageSize { get; set; } = 10;

    private CountryCode _legalEntityCountryCode { get; set; }

    [Parameter]
    public List<SignatoryContact> SignatoryContacts { get; set; }

    [Parameter]
    public bool IsAccountEdit { get; set; }

    [Parameter]
    public Account BillingCustomerAccount { get; set; }

    [Parameter]
    public Account GeneratorAccount { get; set; }
    //Events

    [Parameter]
    public EventCallback<SignatoryContact> SignatoryContactDeleted { get; set; }

    [Parameter]
    public EventCallback<SignatoryContact> SignatoryContactAddUpdate { get; set; }

    [Parameter]
    public EventCallback<SignatoryContact> SignatoryContactAuthorizedChange { get; set; }

    [Parameter]
    public bool IsNewCustomerCreated { get; set; }

    [Inject]
    public IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private bool _isSaveDisabled => !_validBillingCustomerContacts.Any() || !_validBillingCustomerContacts.Any();

    protected override Task OnInitializedAsync()
    {
        _accounts = new()
        {
            BillingCustomerAccount,
            GeneratorAccount,
        };

        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadSignatoryContacts();
        _accountContacts = new();
        _accountContactToAccountMap = new();

        if (BillingCustomerAccount?.Id == GeneratorAccount?.Id)
        {
            _isAccountCustomerAndGenerator = true;
        }

        if (BillingCustomerAccount != null && _validBillingCustomerContacts.Count > 0)
        {
            foreach (var customerContact in _validBillingCustomerContacts)
            {
                if (customerContact.ContactFunctions == null || !customerContact.ContactFunctions.Contains(AccountContactFunctions.FieldSignatoryContact.ToString()))
                {
                    continue;
                }

                if (!SignatoryContacts.Exists(x => x.AccountContactId == customerContact.Id))
                {
                    _accountContacts.Add(customerContact);
                    _accountContactToAccountMap.TryAdd(customerContact.Id, BillingCustomerAccount.Id);
                }
            }
        }

        if (GeneratorAccount != null && _validGeneratorContacts.Count > 0 && !_isAccountCustomerAndGenerator)
        {
            foreach (var generatorContact in _validGeneratorContacts)
            {
                if (!generatorContact.ContactFunctions.Contains(AccountContactFunctions.FieldSignatoryContact.ToString()))
                {
                    continue;
                }

                if (!SignatoryContacts.Exists(x => x.AccountContactId == generatorContact.Id))
                {
                    _accountContacts.Add(generatorContact);
                    _accountContactToAccountMap.TryAdd(generatorContact.Id, GeneratorAccount.Id);
                }
            }
        }

        if (BillingCustomerAccount != null && BillingCustomerAccount.LegalEntityId != default && _legalEntityCountryCode == CountryCode.Undefined)
        {
            //Capture CountryCode from LegalEntity
            var legalEntity = await LegalEntityService.GetById(BillingCustomerAccount.LegalEntityId);
            _legalEntityCountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;
        }
    }

    private Task LoadSignatoryContacts()
    {
        _signatoryContacts = new(SignatoryContacts);
        return Task.CompletedTask;
    }

    private async Task AuthorizeSignatory(SignatoryContact signatoryApplicant)
    {
        await SignatoryContactAuthorizedChange.InvokeAsync(signatoryApplicant);
    }

    private async Task AddSignatoryContact()
    {
        await OpenEditDialog(new() { Id = Guid.NewGuid() }, false);
    }

    private async Task OpenEditDialog(SignatoryContact model, bool editMode = true)
    {
        await DialogService.OpenAsync<BillingApplicantSignatoryEdit>("Add Signatory Contact",
                                                                     new()
                                                                     {
                                                                         { nameof(BillingApplicantSignatoryEdit.signatoryContactModel), model },
                                                                         { nameof(BillingApplicantSignatoryEdit.AccountContacts), _accountContacts },
                                                                         { nameof(BillingApplicantSignatoryEdit.SaveButtonDisabled), !HasWritePermission(Permissions.Resources.Account) },
                                                                         { nameof(BillingApplicantSignatoryEdit.AccountContactToAccountMap), _accountContactToAccountMap },
                                                                         {
                                                                             nameof(BillingApplicantSignatoryEdit.OnSubmit),
                                                                             new EventCallback<SignatoryContact>(this, (Func<SignatoryContact, Task>)(async model =>
                                                                                                                         {
                                                                                                                             DialogService.Close();
                                                                                                                             await SignatoryContactAddUpdate.InvokeAsync(model);
                                                                                                                         }))
                                                                         },
                                                                         { nameof(BillingApplicantSignatoryEdit.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                         {
                                                                             nameof(BillingApplicantSignatoryEdit.OnCreateNewContactSelected), new EventCallback(this, async () =>
                                                                             {
                                                                                 DialogService.Close();
                                                                                 await CreateNewSignatory();
                                                                             })
                                                                         },
                                                                     });
    }

    private async Task CreateNewSignatory(AccountContact model = null)
    {
        _contactViewModel = new(model?.Clone() ?? new AccountContact
        {
            AccountContactAddress = new() { Country = _legalEntityCountryCode },
            ContactFunctions = new() { AccountContactFunctions.FieldSignatoryContact.ToString() },
            IsDeleted = false,
            IsActive = true,
        }, BillingCustomerAccount);

        await DialogService.OpenAsync<AddEditAccountContact>(_contactViewModel.Title,
                                                             new()
                                                             {
                                                                 { nameof(AddEditAccountContact.ViewModel), _contactViewModel },
                                                                 {
                                                                     nameof(AddEditAccountContact.OnSubmit),
                                                                     new EventCallback<AccountContact>(this, (Func<AccountContact, Task>)(async model => await UpdateAccountContact(model)))
                                                                 },
                                                                 { nameof(AddEditAccountContact.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                             },
                                                             new()
                                                             {
                                                                 Width = "60%",
                                                             });
    }

    private async Task UpdateAccountContact(AccountContact contact)
    {
        if (contact.Id == default)
        {
            contact.Id = Guid.NewGuid();
        }

        BillingCustomerAccount.Contacts.Add(contact);
        if (IsAccountEdit)
        {
            var response = await AccountService.Update(BillingCustomerAccount);
            if (!response.IsSuccessStatusCode)
            {
                NotificationService.Notify(NotificationSeverity.Error, detail: "Failed to create Signatory.");
                return;
            }
        }

        NotificationService.Notify(NotificationSeverity.Success, detail: "Signatory created.");

        if (contact.ContactFunctions.Contains(AccountContactFunctions.FieldSignatoryContact.ToString()))
        {
            var newSignatory = new SignatoryContact
            {
                AccountId = IsNewCustomerCreated ? BillingCustomerAccount.Id : GeneratorAccount.Id,
                IsAuthorized = true,
                AccountContactId = contact.Id,
                FirstName = contact.Name,
                LastName = contact.LastName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                Address = contact.Address,
            };

            await SignatoryContactAddUpdate.InvokeAsync(newSignatory);
        }

        DialogService.Close();
    }

    private async Task DeleteButton_Click(SignatoryContact model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Signatory Contact Record";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await SignatoryContactDeleted.InvokeAsync(model);
        }
    }
}
