using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Configuration;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Customers;

public partial class NewCustomerCreditReviewalDialog : BaseRazorComponent
{
    private List<string> _selectedEmails = new();

    private bool IsSaving;

    protected RadzenTemplateForm<NewCustomerCreditReviewalDialogViewModel> ReferenceToForm;

    [Parameter]
    public NewCustomerCreditReviewalDialogViewModel ViewModel { get; set; } = new();

    [Parameter]
    public EventCallback<TruckTicketStubCreationRequest> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private IAccountService AccountService { get; set; }

    [Inject]
    public IAppSettings AppSettings { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private bool IsSaveEnabled { get; set; }

    private async Task HandleCancel()
    {
        DialogService.Close();
        await OnCancel.InvokeAsync();
    }

    private async Task OnOKButtonClick()
    {
        var initiateAccountCreditReviewalRequest = new InitiateAccountCreditReviewalRequest
        {
            AccountId = ViewModel.Account.Id,
            ToEmail = AppSettings["Values:InitiateCreditReviewalEmail"],
            CcEmails = ViewModel.SelectedEmails.ToList(),
        };

        IsSaving = true;
        var response = await AccountService.InitiateAccountCreditRenewal(initiateAccountCreditReviewalRequest);
        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Successfully sent credit renewal request to recipient/s.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Unable to send credit renewal request to recipient/s.");
        }

        IsSaving = false;

        DialogService.Close();
    }

    public async Task OnChangeEmail()
    {
        IsSaveEnabled = !ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    private async Task OnAddEmailTextBoxClick()
    {
        if (!string.IsNullOrEmpty(ViewModel.AddEmailAddressValue))
        {
            if (!ViewModel.SelectedEmails.Contains(ViewModel.AddEmailAddressValue))
            {
                ViewModel.ToEmailAddressList.Add(new()
                {
                    Email = ViewModel.AddEmailAddressValue,
                    DisplayName = ViewModel.AddEmailAddressValue,
                });

                _selectedEmails.Add(ViewModel.AddEmailAddressValue);
                ViewModel.SelectedEmails = _selectedEmails;
                ViewModel.AddEmailAddressValue = null;
            }
        }

        await Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ViewModel.Account?.Contacts != null)
        {
            ViewModel.ToEmailAddressList = ViewModel.Account.Contacts
                                                    .Select(x => new DisplayEmailAddress
                                                     {
                                                         DisplayName = x.DisplayName,
                                                         Email = x.Email,
                                                     }).ToList();

            _selectedEmails = ViewModel.ToEmailAddressList.Select(x => x.Email).ToList();
            ViewModel.SelectedEmails = _selectedEmails;
        }
    }
}

public class NewCustomerCreditReviewalDialogViewModel
{
    public Account Account { get; set; }

    public List<DisplayEmailAddress> ToEmailAddressList { get; set; }

    public IEnumerable<string> SelectedEmails { get; set; }

    public string AddEmailAddressValue { get; set; }
}

public class DisplayEmailAddress
{
    public string DisplayName { get; set; }

    public string Email { get; set; }
}
