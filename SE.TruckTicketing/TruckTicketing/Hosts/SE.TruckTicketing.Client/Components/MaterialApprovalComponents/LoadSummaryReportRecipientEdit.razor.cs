using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class LoadSummaryReportRecipientEdit : BaseRazorComponent
{
    [Parameter]
    public LoadSummaryReportRecipient reportRecipient { get; set; } = new();

    [Parameter]
    public List<AccountContactMap> AccountContacts { get; set; }

    [Parameter]
    public EventCallback<LoadSummaryReportRecipient> OnSubmit { get; set; }

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
        await OnSubmit.InvokeAsync(reportRecipient);
    }

    private void OnChange(object value)
    {
        var changeRecord = AccountContacts.Where(x => x.Contact.Id == (Guid)value).FirstOrDefault(new AccountContactMap());
        reportRecipient.ReportRecipientName = changeRecord.Contact.DisplayName;
        reportRecipient.JobTitle = changeRecord.Contact.JobTitle;
        reportRecipient.ReceiveLoadSummary = true;
        reportRecipient.PhoneNumber = changeRecord.Contact.PhoneNumber;
        reportRecipient.Email = changeRecord.Contact.Email;
        reportRecipient.AccountName = changeRecord.AccountName;
    }
}
