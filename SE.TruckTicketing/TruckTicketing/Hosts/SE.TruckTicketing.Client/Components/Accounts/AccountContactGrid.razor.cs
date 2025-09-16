using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class AccountContactGrid
{
    private SearchResultsModel<AccountContact, SearchCriteriaModel> _accountContacts = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<AccountContact>(),
    };

    private AccountContact model = new();

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Parameter]
    public List<AccountContact> AccountContacts { get; set; }

    //Events

    [Parameter]
    public EventCallback<AccountContact> AccountContactDeleted { get; set; }

    [Parameter]
    public EventCallback<AccountContact> AccountContactAddedUpdated { get; set; }

    private EventCallback<AccountContact> AddAccountAddressHandler =>
        new(this, (Func<AccountContact, Task>)(async model =>
                                               {
                                                   DialogService.Close();
                                                   await AccountContactAddedUpdated.InvokeAsync(model);
                                               }));

    protected override async Task OnParametersSetAsync()
    {
        await LoadAccountContacts();
        await base.OnParametersSetAsync();
    }

    private async Task LoadAccountContacts()
    {
        _accountContacts = new(AccountContacts);
        _accountContacts.Info.TotalRecords = AccountContacts.Count;
        await Task.CompletedTask;
    }

    private async Task DeleteButton_Click(AccountContact model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Account Address";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await AccountContactDeleted.InvokeAsync(model);
        }
    }

    private async Task AddNewContact()
    {
        await OpenEditDialog(new());
    }

    private async Task OpenEditDialog(AccountContact model)
    {
        await DialogService.OpenAsync<AccountContactAddEdit>("Add New Contact",
                                                             new()
                                                             {
                                                                 { "AccountContact", model },
                                                                 { nameof(AccountContactAddEdit.OnSubmit), AddAccountAddressHandler },
                                                                 { nameof(AccountContactAddEdit.OnCancel), HandleCancel },
                                                             });
    }
}
