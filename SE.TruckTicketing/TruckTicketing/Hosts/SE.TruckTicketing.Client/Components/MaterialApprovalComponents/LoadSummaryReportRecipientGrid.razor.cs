using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class LoadSummaryReportRecipientGrid : BaseRazorComponent
{
    private List<AccountContactMap> _accountContacts;

    private SearchResultsModel<LoadSummaryReportRecipient, SearchCriteriaModel> _reportRecipients = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<LoadSummaryReportRecipient>(),
    };

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Parameter]
    public List<LoadSummaryReportRecipient> ReportRecipients { get; set; }

    [Parameter]
    public List<AccountContactMap> AccountContactList { get; set; }

    ////Events

    [Parameter]
    public EventCallback<LoadSummaryReportRecipient> ReceiveLoadSummaryChange { get; set; }

    [Parameter]
    public EventCallback<LoadSummaryReportRecipient> ReportRecipientDeleted { get; set; }

    [Parameter]
    public EventCallback<LoadSummaryReportRecipient> NewReportRecipientAdded { get; set; }

    private EventCallback<LoadSummaryReportRecipient> ConfigureReportRecipientHandler =>
        new(this, (Func<LoadSummaryReportRecipient, Task>)(async model =>
                                                           {
                                                               DialogService.Close();
                                                               await NewReportRecipientAdded.InvokeAsync(model);
                                                           }));

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        LoadReportRecipients(new() { PageSize = 10 });
        _accountContacts = new();

        if (AccountContactList?.Count > 0)
        {
            foreach (var contactList in AccountContactList)
            {
                if (!ReportRecipients.Exists(x => x.AccountContactId == contactList.Contact.Id))
                {
                    _accountContacts.Add(contactList);
                }
            }
        }
    }

    private void LoadReportRecipients(SearchCriteriaModel current)
    {
        var contacts = ReportRecipients ?? new();
        if (!string.IsNullOrEmpty(current.Keywords))
        {
            var lowerKeyword = current.Keywords.ToLower();
            contacts = contacts.Where(x => (x.AccountName.HasText() && x.AccountName.ToLower().Contains(lowerKeyword)) ||
                                           (x.ReportRecipientName.HasText() && x.ReportRecipientName.ToLower().Contains(lowerKeyword))).ToList();
        }

        _reportRecipients = contacts.ToList().CollectionFilterByKeywords(current);
    }

    private async Task ReceiveLoadSummaryUpdateForReportRecipient(LoadSummaryReportRecipient applicantSignatory)
    {
        await ReceiveLoadSummaryChange.InvokeAsync(applicantSignatory);
    }

    private async Task AddApplicantSignatory()
    {
        await DialogService.OpenAsync<LoadSummaryReportRecipientEdit>("Load Summary Report Recipient",
                                                                      new()
                                                                      {
                                                                          { nameof(LoadSummaryReportRecipientEdit.AccountContacts), _accountContacts },
                                                                          { nameof(LoadSummaryReportRecipientEdit.OnSubmit), ConfigureReportRecipientHandler },
                                                                          { nameof(LoadSummaryReportRecipientEdit.OnCancel), HandleCancel },
                                                                      });
    }

    private async Task DeleteButton_Click(LoadSummaryReportRecipient model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Report Recipient Record";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await ReportRecipientDeleted.InvokeAsync(model);
        }
    }
}
