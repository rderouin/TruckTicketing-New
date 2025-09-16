using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.BillingControls;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Pages.BillingConfig;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketBilling : BaseTruckTicketingComponent
{
    private RadzenDropDownDataGrid<Guid?> _billingConfigurationDropdown;

    private List<BillingConfiguration> _billingConfigurations = new();

    private AccountContactDropDown<Guid?> _billingContactDropdown;

    private Account _billingCustomer = new();

    private dynamic _billingServiceDialog;

    private AccountsDropDown<Guid> _customerDropdown = new();

    private EDIValueEditor _ediValueEditor;

    private bool IsSettingCustomer;

    private bool UseCache => !ViewModel.IsRefresh;

    [Inject]
    public TruckTicketExperienceViewModel ViewModel { get; set; }

    public TruckTicket Model => ViewModel.TruckTicket;

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public IServiceProxyBase<BillingConfiguration, Guid> BillingConfigurationService { get; set; }

    public override void Dispose()
    {
        ViewModel.StateChanged -= StateChange;
        ViewModel.Initialized -= StateChange;
    }

    protected override void OnInitialized()
    {
        ViewModel.StateChanged += StateChange;
        ViewModel.Initialized += StateChange;
    }

    protected async Task StateChange()
    {
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleBillingConfigurationChange(object _)
    {
        var billingConfig = _billingConfigurationDropdown.SelectedItem as BillingConfiguration;
        await ViewModel.SetBillingConfiguration(billingConfig);
        StateHasChanged();
    }

    protected async Task HandleBillingConfigurationSave()
    {
        var billingConfiguration = new BillingConfiguration
        {
            BillingCustomerAccountId = Model.BillingCustomerId,
            BillingContactId = Model.BillingContact.AccountContactId,
            BillingConfigurationEnabled = true,
            CustomerGeneratorId = Model.GeneratorId,
            CustomerGeneratorName = Model.GeneratorName,
            StartDate = DateTimeOffset.UtcNow,
            EDIValueData = Model.EdiFieldValues,
            Signatories = Model.Signatories.Select(s => new SignatoryContact
            {
                IsAuthorized = true,
                Address = s.ContactAddress,
                PhoneNumber = s.ContactPhoneNumber,
                FirstName = s.ContactName.Split().First(),
                LastName = s.ContactName.Split().Last(),
                AccountContactId = s.AccountContactId,
                Email = s.ContactEmail,
            }).ToList(),
        };

        var response = await BillingConfigurationService.Create(billingConfiguration);
        if (response.IsSuccessStatusCode)
        {
            Model.BillingConfigurationId = response.Model.Id;
            _billingConfigurations.Add(response.Model);
            Model.IsBillingInfoOverridden = false;
            NotificationService.Notify(NotificationSeverity.Info, "Success", "Custom billing configuration saved.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Something went wrong while trying to save the custom billing configuration.");
        }
    }

    private void HandleBillingCustomerLoad(Account account)
    {
        ViewModel.SetBillingCustomer(account);
        _billingCustomer = account;
    }

    protected void HandleBillingCustomerSelect(Account customer)
    {
        IsSettingCustomer = true;
        _billingCustomer = customer;
        Model.BillingCustomerId = customer.Id;
        Model.BillingCustomerName = customer.Name;
        Model.BillingContact = new();
        Model.EdiFieldValues = new();
        Model.Signatories = new();

        Model.IsBillingInfoOverridden = true;

        ViewModel.SetBillingCustomer(customer);
        IsSettingCustomer = false;
    }

    protected void HandleBillingContactLoad(AccountContactIndex contact)
    {
        ViewModel.BillingCustomerAddress = contact.Address;
        if (!Model.BillingContact?.Name.HasText() ?? true)
        {
            ViewModel.SetBillingContact(contact);
        }
    }

    protected void HandleBillingContactSelect(AccountContactIndex contact)
    {
        ViewModel.SetBillingContact(contact);
        Model.IsBillingInfoOverridden = true;
    }

    protected async Task HandleBillingConfigurationReset()
    {
        await ViewModel.SetBillingConfiguration(_billingConfigurationDropdown.SelectedItem as BillingConfiguration);
    }

    protected void HandleEdiValueChange(List<EDIFieldValue> values)
    {
        ViewModel.SetEdiValues(new(values), _ediValueEditor.IsValid);
        Model.IsBillingInfoOverridden = true;
    }

    protected void HandleSignatoryContactAdd(Signatory signatory)
    {
        Model.Signatories.Add(signatory);
        Model.Signatories = new(Model.Signatories);

        Model.IsBillingInfoOverridden = true;
    }

    protected void HandleSignatoryContactDelete(Signatory signatory)
    {
        Model.Signatories.Remove(signatory);
        Model.Signatories = new(Model.Signatories);

        Model.IsBillingInfoOverridden = true;
    }

    private async Task OpenBillingDialog(Guid id = default)
    {
        var billingConfig = id == Guid.Empty
                                ? _billingConfigurationDropdown.SelectedItem as BillingConfiguration
                                : _billingConfigurationDropdown.View.Cast<BillingConfiguration>().FirstOrDefault(config => config.Id == id);

        _billingServiceDialog = await DialogService.OpenAsync<BillingConfigurationEdit>("Billing Configuration", new()
        {
            { nameof(BillingConfigurationEdit.Id), billingConfig!.Id },
            { nameof(BillingConfigurationEdit.BillingConfigurationModel), billingConfig },
            { nameof(BillingConfigurationEdit.BillingCustomerId), billingConfig.BillingCustomerAccountId.ToString() },
            { nameof(BillingConfigurationEdit.InvoiceConfigurationId), billingConfig.InvoiceConfigurationId },
            { nameof(BillingConfigurationEdit.Operation), "edit" },
            { nameof(BillingConfigurationEdit.HideReturnToAccount), true },
            { nameof(BillingConfigurationEdit.AddEditBillingConfiguration), new EventCallback<BillingConfiguration>(this, UpdatedBillingConfiguration) },
            { nameof(BillingConfigurationEdit.CancelAddEditBillingConfiguration), new EventCallback<bool>(this, CloseBillingConfigDialog) },
            { nameof(BillingConfigurationEdit.IsDisableSaveAndClose), true },
        }, new()
        {
            Width = "80%",
            Height = "95%",
        });
    }

    private void CloseBillingConfigDialog(bool isCanceled)
    {
        DialogService.Close();
    }

    private void UpdatedBillingConfiguration(BillingConfiguration billingConfig)
    {
        DialogService.Close();
    }
}
