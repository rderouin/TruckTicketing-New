using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class AutoPopulateContactAddress : BaseTruckTicketingComponent
{
    private List<AccountAddress> _accountAddresses = new();

    private AccountAddress _selectedAccountAddress = new();

    private Guid? _selectedAddressGuid;

    private bool DisableSelect => _selectedAddressGuid == null || _selectedAddressGuid == Guid.Empty;

    [Parameter]
    public Account CurrentAccount { get; set; }

    [Parameter]
    public EventCallback<AccountAddress> OnSelect { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (CurrentAccount is { AccountAddresses: { } } && CurrentAccount.AccountAddresses.Any())
        {
            _accountAddresses = new(CurrentAccount.AccountAddresses.Where(x => !x.IsDeleted && x.Province != StateProvince.Unspecified));
        }

        return base.OnInitializedAsync();
    }

    private void OnAddressSelect()
    {
        _selectedAccountAddress = _accountAddresses?.FirstOrDefault(x => _selectedAddressGuid != null && x.Id == _selectedAddressGuid.Value, new());
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSelect()
    {
        await OnSelect.InvokeAsync(_selectedAccountAddress);
    }
}
