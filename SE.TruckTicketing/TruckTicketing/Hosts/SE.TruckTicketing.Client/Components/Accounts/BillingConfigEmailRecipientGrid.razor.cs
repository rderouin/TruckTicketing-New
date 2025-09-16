using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.Accounts.Edit;
using SE.TruckTicketing.Client.Components.BillingConfigurationComponents;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Api.Search;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class BillingConfigEmailRecipientGrid
{
    private List<AccountContact> _accountContacts;

    private SearchResultsModel<EmailDeliveryContact, SearchCriteriaModel> _associatedEmailDeliveryContacts = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<EmailDeliveryContact>(),
    };

    private AccountContactViewModel _contactViewModel;

    private int? _pageSize { get; set; } = 10;

    private CountryCode _legalEntityCountryCode { get; set; }

    [Parameter]
    public Account BillingCustomer { get; set; }

    [Parameter]
    public List<EmailDeliveryContact> EmailDeliveryContacts { get; set; }

    [Parameter]
    public List<AccountContact> BillingCustomerAccountContactList { get; set; }
    //Events

    [Parameter]
    public EventCallback<EmailDeliveryContact> EmailDeliveryContactDeleted { get; set; }

    [Parameter]
    public EventCallback<EmailDeliveryContact> NewEmailDeliveryAdded { get; set; }

    [Inject]
    public IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        LoadEmailDeliveryContacts();
        _accountContacts = new();

        if (BillingCustomerAccountContactList.Count > 0)
        {
            foreach (var contactList in BillingCustomerAccountContactList)
            {
                if (!EmailDeliveryContacts.Exists(x => x.AccountContactId == contactList.Id))
                {
                    _accountContacts.Add(contactList);
                }
            }
        }

        if (BillingCustomer.LegalEntityId != default && _legalEntityCountryCode == CountryCode.Undefined)
        {
            //Capture CountryCode from LegalEntity
            var legalEntity = await LegalEntityService.GetById(BillingCustomer.LegalEntityId);
            _legalEntityCountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;
        }
    }

    private void LoadEmailDeliveryContacts()
    {
        _associatedEmailDeliveryContacts = new(EmailDeliveryContacts);
        _associatedEmailDeliveryContacts.Info.TotalRecords = EmailDeliveryContacts.Count;
    }

    private async Task AddEmailRecipient()
    {
        await DialogService.OpenAsync<EmailDeliveryEdit>("Email Delivery",
                                                         new()
                                                         {
                                                             { "CustomerBillingAccountContact", _accountContacts },
                                                             {
                                                                 nameof(EmailDeliveryEdit.OnSubmit), new EventCallback<EmailDeliveryContact>(this, (Func<EmailDeliveryContact, Task>)(async model =>
                                                                             {
                                                                                 DialogService.Close();
                                                                                 await NewEmailDeliveryAdded.InvokeAsync(model);
                                                                             }))
                                                             },
                                                             { nameof(EmailDeliveryEdit.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                             {
                                                                 nameof(EmailDeliveryEdit.OnCreateNewContactSelected), new EventCallback(this, async () =>
                                                                 {
                                                                     DialogService.Close();
                                                                     await CreateNewEmailRecipient();
                                                                 })
                                                             },
                                                         });
    }

    private async Task CreateNewEmailRecipient(AccountContact model = null)
    {
        _contactViewModel = new(model?.Clone() ?? new AccountContact
        {
            AccountContactAddress = new() { Country = _legalEntityCountryCode },
            IsActive = true,
        }, BillingCustomer);

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
        else
        {
            BillingCustomer.Contacts
                           .Remove(BillingCustomer.Contacts.First(x => x.Id ==
                                                                       contact.Id));
        }

        BillingCustomer.Contacts.Add(contact);
        DialogService.Close();
        var newEmailDeliveryContact = new EmailDeliveryContact
        {
            AccountContactId = contact.Id,
            SignatoryContact = contact.Name,
            EmailAddress = contact.Email,
            IsAuthorized = true,
        };

        await NewEmailDeliveryAdded.InvokeAsync(newEmailDeliveryContact);
        await Task.CompletedTask;
    }

    private async Task DeleteButton_Click(EmailDeliveryContact model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Email Delivery Record";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await EmailDeliveryContactDeleted.InvokeAsync(model);
        }
    }
}
