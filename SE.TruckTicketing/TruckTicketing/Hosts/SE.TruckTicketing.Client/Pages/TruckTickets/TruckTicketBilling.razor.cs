using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketBilling : BaseTruckTicketingComponent
{
    private string _billingConfigDependencies;

    private RadzenDropDown<Guid?> _billingConfigurationDropdown;

    private List<BillingConfiguration> _billingConfigurations = new();

    private RadzenDropDown<Guid?> _billingContactDropdown;

    private Account _billingCustomer = new();

    [CascadingParameter(Name = "TruckTicket")]
    public Contracts.Models.Operations.TruckTicket Model { get; set; }

    [Parameter]
    public EventCallback LoadPreviewSalesLines { get; set; }

    [Inject]
    public ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public IServiceProxyBase<BillingConfiguration, Guid> BillingConfigurationService { get; set; }

    private IEnumerable<AccountContact> BillingContacts =>
        _billingCustomer?
           .Contacts?
           .Where(c => c.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString()))
           .OrderBy(x => x.Name)
           .ToArray() ?? Array.Empty<AccountContact>();

    private string BillingCustomerAddress =>
        _billingCustomer?.AccountAddresses?
           .FirstOrDefault(a => a.IsPrimaryAddress)?
           .Display ?? string.Empty;

    private void HandleBillingCustomerLoading(SearchCriteriaModel criteria)
    {
    }

    private async Task LoadBillingConfigurations()
    {
        if (Model.FacilityId == default ||
            Model.SourceLocationId == default ||
            Model.GeneratorId == default)
        {
            return;
        }

        var dependencies = Model.FacilityId +
                           Model.WellClassification.ToString() +
                           Model.SourceLocationId +
                           Model.FacilityServiceId;

        if (_billingConfigDependencies != dependencies)
        {
            _billingConfigurations = await TruckTicketService.GetMatchingBillingConfiguration(Model) ?? new();
        }

        _billingConfigDependencies = dependencies;
    }

    private async Task HandleBillingConfigurationSave()
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

    private void HandleBillingCustomerLoad(Account customer)
    {
        _billingCustomer = customer;
    }

    private async Task HandleBillingCustomerSelect(Account customer)
    {
        _billingCustomer = customer;
        Model.BillingCustomerId = customer.Id;
        Model.BillingContact = new();
        Model.EdiFieldValues = new();
        Model.Signatories = new();

        Model.IsBillingInfoOverridden = true;

        await LoadPreviewSalesLines.InvokeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadBillingConfigurations();
    }

    private void HandleBillingContactChange()
    {
        Model.BillingContact = _billingContactDropdown.SelectedItem is AccountContact contact
                                   ? new BillingContact
                                   {
                                       Address = contact.Address,
                                       Email = contact.Email,
                                       Name = contact.Name,
                                       PhoneNumber = contact.PhoneNumber,
                                       AccountContactId = contact.Id,
                                   }
                                   : new();

        Model.IsBillingInfoOverridden = true;
    }

    private void HandleBillingConfigurationReset()
    {
        HandleBillingConfigurationChange();

        Model.IsBillingInfoOverridden = false;
    }

    private void HandleEdiValueChange(List<EDIFieldValue> values)
    {
        Model.EdiFieldValues = new(values);
        Model.IsBillingInfoOverridden = true;
    }

    private void HandleSignatoryContactAdd(Signatory signatory)
    {
        Model.Signatories.Add(signatory);
        Model.Signatories = new(Model.Signatories);

        Model.IsBillingInfoOverridden = true;
    }

    private void HandleSignatoryContactDelete(Signatory signatory)
    {
        Model.Signatories.Remove(signatory);
        Model.Signatories = new(Model.Signatories);

        Model.IsBillingInfoOverridden = true;
    }

    private void HandleBillingConfigurationChange()
    {
        var billingConfiguration = _billingConfigurationDropdown.SelectedItem as BillingConfiguration;

        Model.IsBillingInfoOverridden = false;

        Model.BillingCustomerId = billingConfiguration?.BillingCustomerAccountId ?? default;

        Model.BillingContact = new()
        {
            AccountContactId = billingConfiguration?.BillingContactId,
        };

        Model.EdiFieldValues = billingConfiguration?
                              .EDIValueData
                              .Select(e => e.Clone())
                              .ToList();

        Model.Signatories = billingConfiguration?
                           .Signatories
                           .Where(e => e.IsAuthorized)
                           .Select(e => new Signatory
                            {
                                AccountContactId = e.AccountContactId,
                                ContactEmail = e.Email,
                                ContactPhoneNumber = e.PhoneNumber,
                                ContactAddress = e.Address,
                                ContactName = e.FirstName + " " + e.LastName,
                            })
                           .ToList();
    }

    private string GetBillingConfigurationListClass(bool isEdiValid)
    {
        return isEdiValid ? "" : "text-danger";
    }
}
