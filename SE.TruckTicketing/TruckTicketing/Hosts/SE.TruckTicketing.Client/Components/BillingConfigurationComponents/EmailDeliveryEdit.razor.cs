using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.BillingConfigurationComponents;

public partial class EmailDeliveryEdit : BaseRazorComponent
{
    [Parameter]
    public EmailDeliveryContact emailModel { get; set; } = new();

    [Parameter]
    public List<AccountContact> CustomerBillingAccountContact { get; set; }

    [Parameter]
    public EventCallback<EmailDeliveryContact> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }
    [Parameter]
    public EventCallback OnCreateNewContactSelected { get; set; }
    [Parameter]
    public Guid BillingCustomerId { get; set; }

    private bool _isSaveDisabled => !CustomerBillingAccountContact.Any();

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(emailModel);
    }

    private void OnChange(object value)
    {
        var changeRecord = CustomerBillingAccountContact.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        emailModel.AccountContactId = changeRecord.Id;
        emailModel.SignatoryContact = changeRecord.Name;
        emailModel.EmailAddress = changeRecord.Email;
        emailModel.IsAuthorized = true;
    }

    private async Task CreateNewEmailRecipient()
    {
        await OnCreateNewContactSelected.InvokeAsync();
    }
}
