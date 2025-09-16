using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class AccountAddressesGrid
{
    private SearchResultsModel<AccountAddress, SearchCriteriaModel> _accountAddresses = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<AccountAddress>(),
    };

    private AccountAddress model = new();

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Parameter]
    public List<AccountAddress> AccountAddresses { get; set; }

    [Parameter]
    public CountryCode LegalEntityCountryCode { get; set; }

    //Events

    [Parameter]
    public EventCallback<AccountAddress> DefaultAccountAddressChange { get; set; }

    [Parameter]
    public EventCallback<AccountAddress> AccountAddressDeleted { get; set; }

    [Parameter]
    public EventCallback<AccountAddress> AccountAddressAddedUpdated { get; set; }

    private EventCallback<AccountAddress> AddAccountAddressHandler =>
        new(this, (Func<AccountAddress, Task>)(async model =>
                                               {
                                                   DialogService.Close();
                                                   await AccountAddressAddedUpdated.InvokeAsync(model);
                                               }));

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadAccountAddresses();
    }

    private Task LoadAccountAddresses()
    {
        _accountAddresses = new(AccountAddresses.Where(x => !x.IsDeleted));
        return Task.CompletedTask;
    }

    private async Task DefaultAccountAddressUpdate(bool isPrimaryAccount, AccountAddress accountAddress)
    {
        model = accountAddress;
        await DefaultAccountAddressChange.InvokeAsync(model);
    }

    private async Task DeleteButton_Click(AccountAddress model)
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
            model.IsDeleted = true;
            await AccountAddressDeleted.InvokeAsync(model);
        }
    }

    private async Task AddNewAddress()
    {
        await OpenEditDialog(new() { Country = LegalEntityCountryCode });
    }

    private async Task OpenEditDialog(AccountAddress model)
    {
        var clonedAccountAddress = model.Clone();
        var title = model.Id == default ? "Add New Address" : "Edit Address";
        await DialogService.OpenAsync<AccountAddressAddEdit>(title,
                                                             new()
                                                             {
                                                                 { "AccountAddress", clonedAccountAddress },
                                                                 { nameof(AccountAddressAddEdit.OnSubmit), AddAccountAddressHandler },
                                                                 { nameof(AccountAddressAddEdit.OnCancel), HandleCancel },
                                                             });
    }
}
