using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Accounts.Edit;

public partial class AddEditAccountContact : BaseTruckTicketingComponent
{
    private readonly string EmailValidationPattern = "^((?!\\.)[\\w-_.]*[^.])(@[\\w-]+)(\\.\\w+(\\.\\w+)?[^.\\W])$";

    private Response<Account> _accountWorkflowValidationResponse;

    private bool _isActiveDisabled;

    private List<ValidationResult> _validationErrors = new();

    private bool originalIsPrimaryAccount;

    protected RadzenTemplateForm<AccountContact> ReferenceToForm;

    private bool IsDisableDefaultAddress =>
        ViewModel is { CurrentAccount: { AccountAddresses: { } } } && (!ViewModel.CurrentAccount.AccountAddresses.Any() || !ViewModel.CurrentAccount.AccountAddresses.Any(x => x.IsPrimaryAddress));

    private bool AddressRequired => ViewModel.AccountContact.IsPrimaryAccountContact || ViewModel.AccountContact.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString());

    [Parameter]
    public AccountContactViewModel ViewModel { get; set; } = new(new(), new());

    [Parameter]
    public EventCallback<AccountContact> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<AccountContact> OnDelete { get; set; }

    [Inject]
    public IServiceBase<AccountContactReferenceIndex, Guid> AccountContactReferenceIndexService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private bool IsSubmitDisabled => !ReferenceToForm.EditContext.Validate();

    [Inject]
    private INewAccountService NewAccountService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _isActiveDisabled = ViewModel.IsNew;
        originalIsPrimaryAccount = ViewModel.AccountContact.IsPrimaryAccountContact;
        await base.OnInitializedAsync();
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private bool IsSaving { get; set; } = false;

    private async Task HandleSubmit()
    {
        IsSaving = true;
        var isSuccessfulValidation = await PerformWorkflowValidation();
        if (ViewModel.AccountContact.IsPrimaryAccountContact && ViewModel.IsCustomerAccountType &&
            !ViewModel.AccountContact.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString()))
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: ViewModel.PrimaryAccountContactValidationMessage);
            IsSaving = false;
            return;
        }

        if (isSuccessfulValidation)
        {
            await OnSubmit.InvokeAsync(ViewModel.AccountContact);
        }

        IsSaving = false;
    }

    private async Task HandleDelete()
    {
        var isSuccessfulValidation = await PerformWorkflowValidation(true);
        if (isSuccessfulValidation)
        {
            await OnDelete.InvokeAsync(ViewModel.AccountContact);
        }
    }

    private async Task OnPrimaryAccountFlagChange(bool updatedValue)
    {
        originalIsPrimaryAccount = !updatedValue;
        ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    private async Task<bool> PerformWorkflowValidation(bool isDelete = false)
    {
        var accountClone = ViewModel.CurrentAccount.Clone();
        if (ViewModel.IsNew)
        {
            accountClone.Contacts.Add(ViewModel.AccountContact);
        }
        else
        {
            foreach (var accountContact in accountClone.Contacts.Where(x => x.Id == ViewModel.AccountContact.Id))
            {
                accountContact.Name = ViewModel.AccountContact.Name;
                accountContact.LastName = ViewModel.AccountContact.LastName;
                accountContact.ContactFunctions = ViewModel.AccountContact.ContactFunctions;
                accountContact.PhoneNumber = ViewModel.AccountContact.PhoneNumber;
                accountContact.IsPrimaryAccountContact = ViewModel.AccountContact.IsPrimaryAccountContact;
                accountContact.Email = ViewModel.AccountContact.Email;
                accountContact.AccountContactAddress = ViewModel.AccountContact.AccountContactAddress;
                accountContact.IsActive = !isDelete && ViewModel.AccountContact.IsActive;
            }
        }

        var response = await NewAccountService.AccountWorkflowValidation(accountClone);

        _validationErrors = JsonConvert.DeserializeObject<List<ValidationResult>>(response.ResponseContent);
        if (_validationErrors != null && _validationErrors.Any(x => x.MemberNames.Any(member => member.StartsWith("contacts"))))
        {
            _isActiveDisabled = true;
            ViewModel.AccountContact.IsActive = true;
            ViewModel.AccountContact.IsPrimaryAccountContact = originalIsPrimaryAccount;
            _accountWorkflowValidationResponse = response;
        }
        else
        {
            _accountWorkflowValidationResponse = new() { StatusCode = HttpStatusCode.Accepted };
            return true;
        }

        return false;
    }

    private string ClassNames(params (string className, bool include)[] classNames)
    {
        var classes = string.Join(" ", (classNames ?? Array.Empty<(string className, bool include)>()).Where(_ => _.include).Select(_ => _.className));
        return $"{classes}";
    }

    private async Task AutoPopulateAddress()
    {
        //Open Dialog to auto-populate contact address from account addresses
        await DialogService.OpenAsync<AutoPopulateContactAddress>("Select Contact Address",
                                                                  new()
                                                                  {
                                                                      { nameof(AutoPopulateContactAddress.CurrentAccount), ViewModel.CurrentAccount },
                                                                      { nameof(AutoPopulateContactAddress.OnSelect), new EventCallback<AccountAddress>(this, HandleSelectedAddress) },
                                                                      { nameof(AutoPopulateContactAddress.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                  },
                                                                  new()
                                                                  {
                                                                      Width = "50%",
                                                                  });
    }

    private void AutoPopulateDefaultAddress()
    {
        var defaultAddress = ViewModel?.CurrentAccount?.AccountAddresses?.FirstOrDefault(address => address.IsPrimaryAddress && !address.IsDeleted, new());
        if (defaultAddress != null)
        {
            PopulateAccountContactAddress(defaultAddress);
        }
    }

    private void HandleSelectedAddress(AccountAddress selectedAccountAddress)
    {
        DialogService.Close();
        PopulateAccountContactAddress(selectedAccountAddress);
    }

    private void PopulateAccountContactAddress(AccountAddress selectedAccountAddress)
    {
        ViewModel.AccountContact.AccountContactAddress.Street = selectedAccountAddress.Street;
        ViewModel.AccountContact.AccountContactAddress.City = selectedAccountAddress.City;
        ViewModel.AccountContact.AccountContactAddress.Country = selectedAccountAddress.Country;
        ViewModel.AccountContact.AccountContactAddress.Province = selectedAccountAddress.Province;
        ViewModel.AccountContact.AccountContactAddress.ZipCode = selectedAccountAddress.ZipCode;
        ViewModel.UpdateStateProvinceLabel(selectedAccountAddress.Country.ToString());
        ViewModel.StateProviceData = ViewModel.StateProvinceDataByCategory[Enum.Parse<CountryCode>(selectedAccountAddress.Country.ToString()).ToString()];
    }

    private class ValidationResult
    {
        public List<string> MemberNames { get; set; }
    }
}
