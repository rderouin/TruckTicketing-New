using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class TruckTicketSignatoryEdit : BaseRazorComponent
{
    [Parameter]
    public Signatory signatoryModel { get; set; } = new();

    [Parameter]
    public List<AccountContact> AccountContacts { get; set; }

    [Parameter]
    public EventCallback<Signatory> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public Guid BillingCustomerId { get; set; }

    [Parameter]
    public Dictionary<Guid, Guid> AccountContactToAccountMap { get; set; }

    private bool _isSaveDisabled => !AccountContacts.Any();

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(signatoryModel);
    }

    private void OnChange(object value)
    {
        var changeRecord = AccountContacts.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        signatoryModel.AccountId = AccountContactToAccountMap[changeRecord.Id];
        signatoryModel.IsAuthorized = true;
        signatoryModel.AccountContactId = changeRecord.Id;
        signatoryModel.ContactName = $"{changeRecord.Name} {changeRecord.LastName}";
        signatoryModel.ContactEmail = changeRecord.Email;
        signatoryModel.ContactPhoneNumber = changeRecord.PhoneNumber;
        signatoryModel.ContactAddress = changeRecord.Address;
    }

    private string GetAccount(AccountContact accountContact)
    {
        return $"{accountContact.Name} {accountContact.LastName}";
    }
}
