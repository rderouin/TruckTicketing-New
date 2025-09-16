using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class ApplicantSignatoryEdit : BaseRazorComponent
{
    [Parameter]
    public ApplicantSignatory applicantSignatory { get; set; } = new();

    [Parameter]
    public List<AccountContactMap> AccountContacts { get; set; }

    [Parameter]
    public EventCallback<ApplicantSignatory> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public Guid BillingCustomerId { get; set; }

    private bool _isSaveDisabled => !AccountContacts.Any();

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(applicantSignatory);
    }

    private void OnChange(object value)
    {
        var changeRecord = AccountContacts.Where(x => x.Contact.Id == (Guid)value).FirstOrDefault(new AccountContactMap());
        applicantSignatory.SignatoryName = changeRecord.Contact.DisplayName;
        applicantSignatory.JobTitle = changeRecord.Contact.JobTitle;
        applicantSignatory.ReceiveLoadSummary = true;
        applicantSignatory.PhoneNumber = changeRecord.Contact.PhoneNumber;
        applicantSignatory.Email = changeRecord.Contact.Email;
        applicantSignatory.AccountName = changeRecord.AccountName;
    }
}
