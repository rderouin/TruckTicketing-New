using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class ReportingInfo : BaseRazorComponent
{
    private EditContext _editContext;

    private bool ShowMonthlyDate;

    private bool ShowWeekDay;

    [Parameter]
    public MaterialApproval model { get; set; }

    [Parameter]
    public List<AccountContactMap> AccountContacts { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    private IEnumerable<int> _MonthDates => Enumerable.Range(1, 31);

    protected override async Task OnInitializedAsync()
    {
        _editContext = new(model);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        OnLoadSummaryFrequencyChange(model.LoadSummaryReportFrequency);
        await base.OnInitializedAsync();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnContextChange.InvokeAsync(e.FieldIdentifier);
    }

    private void OnLoadSummaryFrequencyChange(LoadSummaryReportFrequency loadSummaryReportFrequency)
    {
        ShowMonthlyDate = ShowWeekDay = false;

        if (loadSummaryReportFrequency == LoadSummaryReportFrequency.Monthly)
        {
            ShowMonthlyDate = true;
            model.LoadSummaryReportFrequencyWeekDay = default;
        }
        else if (loadSummaryReportFrequency == LoadSummaryReportFrequency.Weekly)
        {
            ShowWeekDay = true;
            model.LoadSummaryReportFrequencyMonthlyDate = null;
        }

        StateHasChanged();
    }

    private void AddNewReportRecipient(LoadSummaryReportRecipient reportRecipient)
    {
        var reportRecipients = new List<LoadSummaryReportRecipient>(model.LoadSummaryReportRecipients) { reportRecipient };

        model.LoadSummaryReportRecipients = reportRecipients;
        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.LoadSummaryReportRecipients)));
    }

    private void LoadSummaryReportRecipientDeleted(LoadSummaryReportRecipient reportRecipient)
    {
        var reportRecipients = new List<LoadSummaryReportRecipient>(model.LoadSummaryReportRecipients);
        reportRecipients.Remove(reportRecipient);
        model.LoadSummaryReportRecipients = reportRecipients;
        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.LoadSummaryReportRecipients)));
    }

    private void UpdateReportRecipientReceiveLoadSummary(LoadSummaryReportRecipient reportRecipient)
    {
        model.LoadSummaryReportRecipients.Where(x => x.AccountContactId == reportRecipient.AccountContactId).Select(c =>
                                                                                                                    {
                                                                                                                        c.ReceiveLoadSummary = reportRecipient.ReceiveLoadSummary;
                                                                                                                        return c;
                                                                                                                    }).ToList();

        OnContextChange.InvokeAsync(_editContext.Field(nameof(MaterialApproval.LoadSummaryReportRecipients)));
    }
}
