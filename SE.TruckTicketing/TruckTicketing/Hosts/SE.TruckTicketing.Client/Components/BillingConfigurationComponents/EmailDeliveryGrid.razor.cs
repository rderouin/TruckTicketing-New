using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.BillingConfigurationComponents;

public partial class EmailDeliveryGrid
{
    private List<AccountContact> _accountContacts;

    private SearchResultsModel<EmailDeliveryContact, SearchCriteriaModel> _associatedEmailDeliveryContacts = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<EmailDeliveryContact>(),
    };

    private bool isEmailDeliveryContactFieldsDisabled;

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private int? _pageSize { get; set; } = 10;

    [Parameter]
    public List<EmailDeliveryContact> EmailDeliveryContacts { get; set; }

    [Parameter]
    public List<AccountContact> BillingCustomerAccountContactList { get; set; }

    [Parameter]
    public bool Disabled { get; set; }
    //Events

    [Parameter]
    public EventCallback<EmailDeliveryContact> EmailDeliveryContactAuthorizedChange { get; set; }

    [Parameter]
    public EventCallback<EmailDeliveryContact> EmailDeliveryContactDeleted { get; set; }

    [Parameter]
    public EventCallback<EmailDeliveryContact> NewEmailDeliveryAdded { get; set; }

    [Parameter]
    public EventCallback<bool> IsEnableEmailDeliveryContact { get; set; }

    private EventCallback<EmailDeliveryContact> ConfigureEmailContactHandler =>
        new(this, (Func<EmailDeliveryContact, Task>)(async model =>
                                                     {
                                                         //Update _associatedEmailDeliveryContacts
                                                         //1. If new Email added, add new record in the list
                                                         //2. If Edit existing record, update currently passed model with new selection
                                                         //Reload grid to display updated information from Add/Edit
                                                         //Trigger event callback to Parent to enable Create
                                                         DialogService.Close();
                                                         await NewEmailDeliveryAdded.InvokeAsync(model);
                                                     }));

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadEmailDeliveryContacts();
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
    }

    private Task LoadEmailDeliveryContacts()
    {
        _associatedEmailDeliveryContacts = new(EmailDeliveryContacts);
        return Task.CompletedTask;
    }

    private async Task EnableEmailContactAsSignatory(EmailDeliveryContact emailDeliveryContact)
    {
        await EmailDeliveryContactAuthorizedChange.InvokeAsync(emailDeliveryContact);
    }

    private async Task EnableEmailDeliveryGrid(bool isEnabled)
    {
        isEmailDeliveryContactFieldsDisabled = !isEnabled;
        await IsEnableEmailDeliveryContact.InvokeAsync(isEnabled);
    }

    private async Task AddEmailRecipient()
    {
        await DialogService.OpenAsync<EmailDeliveryEdit>("Email Delivery",
                                                         new()
                                                         {
                                                             { "CustomerBillingAccountContact", _accountContacts },
                                                             { nameof(EmailDeliveryEdit.OnSubmit), ConfigureEmailContactHandler },
                                                             { nameof(EmailDeliveryEdit.OnCancel), HandleCancel },
                                                         });
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
