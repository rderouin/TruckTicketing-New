using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class NewAccountBillingConfiguration
{
    public string PostalCodeMask = "*****-****";

    public string PostalCodePattern = "[0-9]";

    protected RadzenTemplateForm<AccountAddress> ReferenceToAccountAddressForm;

    protected RadzenTemplateForm<AccountAddress> ReferenceToAddressForm;

    protected RadzenTemplateForm<AccountAddress> ReferenceToForm;

    public bool SameAsAccountAddress;

    public Dictionary<StateProvince, string> StateProviceData = new();

    private string StateProvinceLabel = "State/Province";

    private string StateProvincePlaceHolder = "Select State/Province...";

    private string ZipPostalCodeLabel = "Zip/Postal Code";

    private List<AccountContact> _billingCustomerContacts => Account.Contacts.Where(x => !x.IsDeleted).ToList();

    private string MailingRecipient
    {
        get => Account.MailingRecipientName;
        set => Account.MailingRecipientName = SameAsAccountAddress ? string.Empty : value;
    }

    [Parameter]
    public Account Account { get; set; }

    [Parameter]
    public Account BillingCustomer { get; set; }

    [Parameter]
    public BillingConfiguration billingConfiguration { get; set; }

    [Parameter]
    public List<EDIFieldDefinition> EdiFieldDefinitions { get; set; }

    [Parameter]
    public AccountAddress MailingAddress { get; set; }

    [Parameter]
    public bool IsNewBillingCusomerCreated { get; set; }

    private IDictionary<string, Dictionary<StateProvince, string>> stateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    public CountryCode SelectedCountry
    {
        get => MailingAddress.Country;
        set
        {
            MailingAddress.Country = value;
            UpdateStateProvinceLabel(value.ToString());
            StateProviceData = stateProvinceDataByCategory[value.ToString()];
        }
    }

    private void MailSameAsAccountAddress(bool sameAsAccountAddress)
    {
        Account.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = sameAsAccountAddress ? AddressType.Mail : default;
        MailingAddress.Id = sameAsAccountAddress ? default : Guid.NewGuid();
        SameAsAccountAddress = sameAsAccountAddress;
    }

    private void OnBillingTypeChange()
    {
        MailingAddress.Id = Account.BillingType == BillingType.Mail ? Guid.NewGuid() : Guid.Empty;
        billingConfiguration.EmailDeliveryContacts = new();
        billingConfiguration.EDIValueData = new();
    }

    private void OnConfigureBillingChange()
    {
        if (Account.IsElectronicBillingEnabled)
        {
            return;
        }

        Account.BillingType = BillingType.Undefined;
        MailingAddress.Id = Guid.Empty;
        billingConfiguration.EmailDeliveryContacts = new();
        billingConfiguration.EDIValueData = new();
    }

    private void EmailContactDeleted(EmailDeliveryContact emailContact)
    {
        var existingEmailContact = new List<EmailDeliveryContact>(billingConfiguration.EmailDeliveryContacts);
        if (existingEmailContact.Any(x => x.AccountContactId == emailContact.AccountContactId))
        {
            existingEmailContact.Remove(existingEmailContact.First(x => x.AccountContactId == emailContact.AccountContactId));
        }

        billingConfiguration.EmailDeliveryContacts = existingEmailContact;
    }

    private void EmailContactAdded(EmailDeliveryContact emailContact)
    {
        var existingEmailContact = new List<EmailDeliveryContact>(billingConfiguration.EmailDeliveryContacts);
        existingEmailContact.Add(emailContact);
        billingConfiguration.EmailDeliveryContacts = existingEmailContact;
    }

    private void DeleteSignatoryContact(SignatoryContact signatoryContact)
    {
        var existingSignatoryContacts = new List<SignatoryContact>(billingConfiguration.Signatories);
        if (existingSignatoryContacts.Any(x => x.AccountContactId == signatoryContact.AccountContactId))
        {
            existingSignatoryContacts.Remove(existingSignatoryContacts.First(x => x.AccountContactId == signatoryContact.AccountContactId));
        }

        billingConfiguration.Signatories = existingSignatoryContacts;
    }

    private void AddSignatoryContact(SignatoryContact signatoryContact)
    {
        var existingSignatoryContacts = new List<SignatoryContact>(billingConfiguration.Signatories);
        existingSignatoryContacts.Add(signatoryContact);
        billingConfiguration.Signatories = existingSignatoryContacts;
    }

    public void UpdateStateProvinceLabel(string value)
    {
        if (Enum.Parse<CountryCode>(value) == CountryCode.CA)
        {
            StateProvincePlaceHolder = "Select Province...";
            StateProvinceLabel = "Province";
            PostalCodeMask = "*** ***";
            PostalCodePattern = "[0-9A-z]";
            ZipPostalCodeLabel = "Postal Code";
        }
        else
        {
            StateProvincePlaceHolder = "Select State...";
            StateProvinceLabel = "State";
            PostalCodeMask = "*****-****";
            PostalCodePattern = "[0-9]";
            ZipPostalCodeLabel = "Zip Code";
        }
    }
}
