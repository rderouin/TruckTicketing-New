using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class SelectExistingBillingCustomer
{
    public Account SelctedCustomer { get; set; } = new();

    [Parameter]
    public EventCallback<Account> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCreateNewCustomer { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(SelctedCustomer);
    }

    private async Task CreateNewCustomer()
    {
        await OnCreateNewCustomer.InvokeAsync();
    }

    private async Task BillingCustomerSelection(Account selectedCustomer)
    {
        SelctedCustomer = selectedCustomer;
        await Task.CompletedTask;
    }
}
