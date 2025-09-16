using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class ApplicantSignatoryGrid : BaseRazorComponent
{
    private List<AccountContactMap> _accountContacts;

    private SearchResultsModel<ApplicantSignatory, SearchCriteriaModel> _applicantSignatories = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<ApplicantSignatory>(),
    };

    private bool IsAddNewSignatory => ApplicantSignatories?.Count(x => x.ReceiveLoadSummary) <= 2;

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Parameter]
    public List<ApplicantSignatory> ApplicantSignatories { get; set; }

    [Parameter]
    public List<AccountContactMap> AccountContactList { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }
    ////Events

    [Parameter]
    public EventCallback<ApplicantSignatory> ReceiveLoadSummaryChange { get; set; }

    [Parameter]
    public EventCallback<ApplicantSignatory> ApplicantSignatoryDeleted { get; set; }

    [Parameter]
    public EventCallback<ApplicantSignatory> NewApplicantSignatoryAdded { get; set; }

    private EventCallback<ApplicantSignatory> ConfigureApplicantSignatoryHandler =>
        new(this, (Func<ApplicantSignatory, Task>)(async model =>
                                                   {
                                                       DialogService.Close();
                                                       await NewApplicantSignatoryAdded.InvokeAsync(model);
                                                   }));

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        LoadApplicantSignatoryContacts(new() { PageSize = 10 });
        _accountContacts = new();

        if (AccountContactList?.Count > 0)
        {
            foreach (var contactList in AccountContactList)
            {
                if (!ApplicantSignatories.Exists(x => x.AccountContactId == contactList.Contact.Id))
                {
                    _accountContacts.Add(contactList);
                }
            }
        }
    }

    private void LoadApplicantSignatoryContacts(SearchCriteriaModel current)
    {
        var contacts = ApplicantSignatories ?? new();
        if (!string.IsNullOrEmpty(current.Keywords))
        {
            var lowerKeyword = current.Keywords.ToLower();
            contacts = contacts.Where(x => (x.AccountName.HasText() && x.AccountName.ToLower().Contains(lowerKeyword)) ||
                                           (x.SignatoryName.HasText() && x.SignatoryName.ToLower().Contains(lowerKeyword)) ||
                                           (x.ReferenceId == current.Keywords)).ToList();
        }

        _applicantSignatories = contacts.ToList().CollectionFilterByKeywords(current);
    }

    private async Task ReceiveLoadSummaryUpdateForApplicantSignatory(ApplicantSignatory applicantSignatory)
    {
        await ReceiveLoadSummaryChange.InvokeAsync(applicantSignatory);
    }

    private async Task AddApplicantSignatory()
    {
        await DialogService.OpenAsync<ApplicantSignatoryEdit>("Add Signatory",
                                                              new()
                                                              {
                                                                  { nameof(ApplicantSignatoryEdit.AccountContacts), _accountContacts },
                                                                  { nameof(ApplicantSignatoryEdit.OnSubmit), ConfigureApplicantSignatoryHandler },
                                                                  { nameof(ApplicantSignatoryEdit.OnCancel), HandleCancel },
                                                              });
    }

    private async Task DeleteButton_Click(ApplicantSignatory model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Applicant Signatory Record";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await ApplicantSignatoryDeleted.InvokeAsync(model);
        }
    }
}
