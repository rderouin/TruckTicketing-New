using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.BillingConfigurationComponents;

public partial class BillingApplicantSignatoryEdit : BaseRazorComponent
{
    [Parameter]
    public SignatoryContact signatoryContactModel { get; set; } = new();

    [Parameter]
    public List<AccountContact> AccountContacts { get; set; }

    [Parameter]
    public EventCallback<SignatoryContact> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public Guid BillingCustomerId { get; set; }

    [Parameter]
    public Dictionary<Guid, Guid> AccountContactToAccountMap { get; set; }

    [Parameter]
    public EventCallback OnCreateNewContactSelected { get; set; }

    [Parameter]
    public bool AllowCreateNewContact { get; set; }

    [Parameter]
    public bool SaveButtonDisabled { get; set; }

    private bool _isSaveDisabled => !AccountContacts.Any() || SaveButtonDisabled;

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(signatoryContactModel);
    }

    private void OnChange(object value)
    {
        var changeRecord = AccountContacts.Where(x => x.Id == (Guid)value).FirstOrDefault(new AccountContact());
        if(!AccountContactToAccountMap.TryGetValue(changeRecord.Id, out var accountId))
        {
            return;
        }
        signatoryContactModel.AccountId = accountId;
        signatoryContactModel.IsAuthorized = true;
        signatoryContactModel.AccountContactId = changeRecord.Id;
        signatoryContactModel.FirstName = changeRecord.Name;
        signatoryContactModel.LastName = changeRecord.LastName;
        signatoryContactModel.Email = changeRecord.Email;
        signatoryContactModel.PhoneNumber = changeRecord.PhoneNumber;
        signatoryContactModel.Address = changeRecord.Address;
    }

    private async Task CreateNewSignatory()
    {
        await OnCreateNewContactSelected.InvokeAsync();
    }
}
