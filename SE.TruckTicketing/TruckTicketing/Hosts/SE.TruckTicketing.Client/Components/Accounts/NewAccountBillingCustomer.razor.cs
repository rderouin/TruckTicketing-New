using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Client.Extensions;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class NewAccountBillingCustomer : BaseRazorComponent
{
    private const string SuccessSummary = "Success: ";

    private const string ErrrorSummary = "Error: ";

    private CountryCode _legalEntityCountryCode = CountryCode.Undefined;

    private string _selectedContactName = String.Empty;

    private string _selectedCustmerPrimaryAddress;

    private AccountContact _selectedCustmerPrimaryContact = new();

    protected RadzenTemplateForm<Account> ReferenceToBillingCustomerGeneralInfoForm;

    private IEnumerable<AccountTypes> SelectedAccountTypes = new List<AccountTypes> { AccountTypes.Customer };

    [Parameter]
    public Account BillingCustomer { get; set; }

    [Parameter]
    public BillingConfiguration billingConfiguration { get; set; }

    [Parameter]
    public AccountAddress BillingCustomerAddress { get; set; }

    [Parameter]
    public RadzenSteps Steps { get; set; }

    [Parameter]
    public bool IsCustomerCreated { get; set; }

    [Parameter]
    public EventCallback<bool> NewCustomerCreated { get; set; }

    [Parameter]
    public EventCallback<Account> SelctedBillingCustomer { get; set; }

    [Inject]
    private NotificationService notificationService { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (BillingCustomer.Id != default)
        {
            var primaryAddress = BillingCustomer.AccountAddresses.Where(x => x.IsPrimaryAddress).FirstOrDefault(new AccountAddress());
            if (primaryAddress != null)
            {
                _selectedCustmerPrimaryAddress = primaryAddress.GetPrimaryAccountAddress();
            }

            _selectedCustmerPrimaryContact = BillingCustomer.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                            .FirstOrDefault(new AccountContact());

            if (_selectedCustmerPrimaryContact != null)
            {
                _selectedContactName = _selectedCustmerPrimaryContact?.Name + _selectedCustmerPrimaryContact?.LastName;
            }
        }

        await base.OnParametersSetAsync();
    }

    protected void HandleLegalEntityLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(LegalEntity.Name);
        criteria.Filters[nameof(LegalEntity.ShowAccountsInTruckTicketing)] = true;
    }

    public async Task OnLegalEntitySelect(LegalEntity legalEntity)
    {
        BillingCustomer.LegalEntity = legalEntity.Code;
        _legalEntityCountryCode = legalEntity.CountryCode;
        await Task.CompletedTask;
    }

    private Task AddEditAccountContact()
    {
        return Task.CompletedTask;
    }

    private void ClickMessage(NotificationSeverity severity, string summary, string detailMessage)
    {
        notificationService.Notify(new()
        {
            Severity = severity,
            Summary = summary,
            Detail = detailMessage,
            Duration = 4000,
        });
    }

    //Update BillingConfiguration with existing selected customer

    private async Task HandleSelectedBillingCustomer(Account selectedCustomer)
    {
        billingConfiguration.BillingCustomerAccountId = selectedCustomer.Id;
        billingConfiguration.IsDefaultConfiguration = true;
        DialogService.Close();
        if (Steps.StepsCollection.Count == 3)
        {
            Steps.RemoveStep(Steps.StepsCollection.Last());
        }

        await NewCustomerCreated.InvokeAsync(false);
        await SelctedBillingCustomer.InvokeAsync(selectedCustomer);

        await Task.CompletedTask;
    }

    private async Task SelectCustomer(MouseEventArgs args)
    {
        await DialogService.OpenAsync<SelectExistingBillingCustomer>("Select Default Billing Customer",
                                                                     new()
                                                                     {
                                                                         { nameof(SelectExistingBillingCustomer.OnCreateNewCustomer), new EventCallback(this, CreateCustomer) },
                                                                         { nameof(SelectExistingBillingCustomer.OnSubmit), new EventCallback<Account>(this, HandleSelectedBillingCustomer) },
                                                                         { nameof(SelectExistingBillingCustomer.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                     });

        await Task.CompletedTask;
    }

    private async Task CreateCustomer()
    {
        billingConfiguration.BillingCustomerAccountId = default;
        IsCustomerCreated = true;
        if (Steps.StepsCollection.Count == 3)
        {
            Steps.RemoveStep(Steps.StepsCollection.Last());
        }

        DialogService.Close();
        await SelctedBillingCustomer.InvokeAsync(new() { Id = default });
        await NewCustomerCreated.InvokeAsync(true);
        await Task.CompletedTask;
    }
}
