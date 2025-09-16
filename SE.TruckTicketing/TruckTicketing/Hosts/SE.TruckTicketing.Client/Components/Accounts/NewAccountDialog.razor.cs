using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class NewAccountDialog : BaseRazorComponent
{
    [Parameter]
    public string AccountType { get; set; }

    [Parameter]
    public EventCallback<Account> AddAccount { get; set; }

    [Parameter]
    public EventCallback CloseDialog { get; set; }

    private async Task OnAddAccount(Account model)
    {
        await AddAccount.InvokeAsync(model);
        DialogService.Close();
    }
    private void IsDialogClose()
    {
        DialogService.Close();
    }
}
