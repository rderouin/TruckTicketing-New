using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class AccountContactAddEdit
{
    protected RadzenTemplateForm<AccountContact> ReferenceToForm;

    [Parameter]
    public AccountContact AccountContact { get; set; } = new();

    [Parameter]
    public EventCallback<AccountContact> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private bool IsSaveDisabled { get; set; } = true;

    private bool IsPrimaryAccount { get; set; }

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(AccountContact);
    }

    public async Task OnChange()
    {
        IsSaveDisabled = !ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    private async Task PrimaryAccountCheck(bool isPrimaryAccount)
    {
        IsPrimaryAccount = isPrimaryAccount;
        IsSaveDisabled = !ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    protected string ClassNames(params (string className, bool include)[] classNames)
    {
        var classes = string.Join(" ", (classNames ?? Array.Empty<(string className, bool include)>()).Where(_ => _.include).Select(_ => _.className));
        return $"{classes}";
    }
}
