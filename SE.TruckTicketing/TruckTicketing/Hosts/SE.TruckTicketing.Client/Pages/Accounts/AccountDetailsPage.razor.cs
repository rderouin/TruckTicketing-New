using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;

using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Contracts.Api.Client;
using Trident.Extensions;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.Client.Pages.Accounts;

public partial class AccountDetailsPage : BaseTruckTicketingComponent
{
    private const string AddBasePath = "/billing-configuration/new/";

    private IEnumerable<AccountContact> _accountContacts;

    private EditContext _editContext;

    private bool _isLoading;

    private bool _isSaving;

    private Response<Account> _response;

    private AccountViewModel _viewModel = new(new());

    private bool DisableCustomerNameTextBox => _viewModel.IsAccountTypeCustomer;

    [Parameter]
    public string Id { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    public IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    private IDictionary<string, Dictionary<StateProvince, string>> stateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    private string BillingTransferRecipientPath => $"/account/edit/{_viewModel.Account.BillingTransferRecipientId}";

    private string ReturnUrl => "/accounts";

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;

        if (Guid.TryParse(Id, out var id))
        {
            await LoadAccount(id);
            _accountContacts = _viewModel.Account.Contacts;
        }
        else
        {
            await LoadAccount();
        }

        if (_viewModel.Account.LegalEntityId != default)
        {
            //Capture CountryCode from LegalEntity
            var legalEntity = await LegalEntityService.GetById(_viewModel.Account.LegalEntityId);
            _viewModel.LegalEntity = legalEntity ?? new();
            _viewModel.LegalEntityCountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;
        }

        _isLoading = false;
    }

    private async Task LoadAccount(Guid? id = null)
    {
        var account = id is null ? new() : await AccountService.GetById(id.Value);
        _viewModel = new(account);
        _editContext = new(account);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _viewModel.SubmitButtonDisabled = !_editContext.IsModified();
    }

    private async Task OnHandleSubmit()
    {
        _isSaving = true;
        if (_viewModel.IsAccountTypeCustomer && _viewModel.Account.Contacts != null && _viewModel.Account.Contacts.Any() && _viewModel.LegalEntity.IsCustomerPrimaryContactRequired)
        {
            var moreThanOnePrimaryContacts = _viewModel.Account.Contacts.Count(x => x.IsPrimaryAccountContact) > 1;
            var noPrimaryContact = _viewModel.Account.Contacts.Count(x => x.IsPrimaryAccountContact) == 0;
            var errorMessage = moreThanOnePrimaryContacts ? "Only one added contact marked as primary contact for the account"
                               : noPrimaryContact ? "Atleast one added contact marked as primary contact for the account" : string.Empty;

            if (moreThanOnePrimaryContacts || noPrimaryContact)
            {
                NotificationService.Notify(NotificationSeverity.Error, detail: errorMessage);
                _isSaving = false;
                return;
            }
        }

        var response = _viewModel.IsNew ? await AccountService.Create(_viewModel.Account) : await AccountService.Update(_viewModel.Account);

        _isSaving = false;

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
            _viewModel.Account = response.Model.Clone();
        }

        _response = response;
    }

    private async Task OnAccountTypeChange(object value)
    {
        _viewModel.Account.AccountTypes = _viewModel.SelectedAccountTypes.Select(x =>
                                                                                 {
                                                                                     return x.GetEnumValue<AccountTypes>();
                                                                                 }).ToList();

        await Task.CompletedTask;
    }

    private void OnOtherInfoContentChange(object value)
    {
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Account)));
    }

    private async Task AddBillingConfiguration()
    {
        NavigationManager.NavigateTo(String.Concat(AddBasePath, _viewModel.Account.Id));
        await Task.CompletedTask;
    }

    private async Task AddInvoiceConfiguration()
    {
        NavigationManager.NavigateTo($"/invoice-configurations/new/{_viewModel.Account.Id}");
        await Task.CompletedTask;
    }

    #region Account Contact Grid

    private Task AddEditAccountContact()
    {
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Account.Contacts)));
        return Task.CompletedTask;
    }

    #endregion

    private async Task CloseButton_Click()
    {
        if (!_editContext.IsModified())
        {
            NavigationManager.NavigateTo(ReturnUrl);
        }

        var confirmation = await DialogService.Confirm("Are you sure you want to close this page?", options: new()
        {
            OkButtonText = "Yes",
            CancelButtonText = "No",
        });

        if (confirmation.GetValueOrDefault())
        {
            NavigationManager.NavigateTo(ReturnUrl);
        }
    }

    #region Account Address List

    private Task AddEditAccountAddress(AccountAddress accountAddress)
    {
        var existingAccountAddresses = new List<AccountAddress>(_viewModel.Account.AccountAddresses);
        if (accountAddress.Id == default)
        {
            accountAddress.Id = Guid.NewGuid();
            existingAccountAddresses.Add(accountAddress);
        }
        else
        {
            var updatedAddress = existingAccountAddresses.FirstOrDefault(x => x.Id == accountAddress.Id, new());
            if (updatedAddress.Id != default)
            {
                var index = existingAccountAddresses.IndexOf(updatedAddress);
                if (index != -1)
                {
                    existingAccountAddresses[index] = accountAddress;
                }
            }
        }

        if (accountAddress.IsPrimaryAddress)
        {
            foreach (var address in existingAccountAddresses.Where(x => x.Id != accountAddress.Id))
            {
                address.IsPrimaryAddress = false;
            }
        }

        _viewModel.Account.AccountAddresses = existingAccountAddresses;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Account.AccountAddresses)));
        return Task.CompletedTask;
    }

    private void UpdateAccountAddressDeleted(AccountAddress accountAddress)
    {
        var existingAccountAddresses = new List<AccountAddress>(_viewModel.Account.AccountAddresses);
        var deletedAddress = existingAccountAddresses.FirstOrDefault(x => x.Id == accountAddress.Id, new());
        if (deletedAddress.Id != default)
        {
            var index = existingAccountAddresses.IndexOf(deletedAddress);
            if (index != -1)
            {
                existingAccountAddresses[index] = accountAddress;
            }
        }

        _viewModel.Account.AccountAddresses = existingAccountAddresses;

        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Account.AccountAddresses)));
    }

    private void UpdateDefaultAccountAddress(AccountAddress accountAddress)
    {
        var existingAccountAddresses = new List<AccountAddress>(_viewModel.Account.AccountAddresses);

        foreach (var address in existingAccountAddresses.Where(x => x.Id != accountAddress.Id))
        {
            address.IsPrimaryAddress = false;
        }

        _viewModel.Account.AccountAddresses = existingAccountAddresses;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Account.AccountAddresses)));
    }

    #endregion
}
