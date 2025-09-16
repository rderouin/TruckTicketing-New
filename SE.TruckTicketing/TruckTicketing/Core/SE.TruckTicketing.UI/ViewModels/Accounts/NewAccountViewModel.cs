using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using AccountTypesEnum = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.UI.ViewModels.Accounts;

public class NewAccountViewModel
{
    public bool IsAccountTypeCustomer;

    public bool IsAccountTypeGenerator;

    public bool IsAccountTypeThirdPartyAnalytical;

    public bool IsAccountTypeTruckingCompany;

    public bool IsBillingCustomer;

    public bool IsEdiFieldsEnabled;

    public bool IsElectronicBillingEnabled;

    public string NextButtonBusyText = "Validating";

    public IEnumerable<AccountTypesEnum> SelectedAccountTypes = new List<AccountTypesEnum>();

    public Dictionary<StateProvince, string> StateProviceData = new();

    public NewAccountViewModel(Account account, Account billingCustomer)
    {
        Account = account;
        BillingCustomer = billingCustomer;
        Breadcrumb = "New Account";
        Title = "Creating Account";
        IsNew = true;

        AccountTypeListData = DataDictionary.For<AccountTypesEnum>().Select(x =>
                                                                                new ListOption<string>
                                                                                {
                                                                                    Display = x.Value,
                                                                                    Value = x.Value,
                                                                                }).ToList();
    }

    public AccountAddress AccountMailingAddress { get; set; } = new()
    {
        AddressType = AddressType.Mail,
    };

    public CountryCode SelectedLegalEntityCountryCode { get; set; }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public Account Account { get; }

    public Account BillingCustomer { get; }

    public BillingConfiguration BillingConfiguration { get; set; } = new()
    {
        Id = Guid.NewGuid(),
        StartDate = GetDefaultStartDate(),
    };

    public bool IsCreateBillingCustomer { get; set; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create" : "Save & Close";

    public string SubmitSuccessNotificationMessage => IsNew ? "Account created." : "Account updated.";

    public string Title { get; }

    public List<ListOption<string>> AccountTypeListData { get; } = new();

    public List<EDIFieldDefinition> EdiFieldDefinitions { get; set; } = new();

    public List<EDIFieldDefinition> EDIFieldDefinitions { get; set; } = new();

    private static DateTimeOffset GetDefaultStartDate()
    {
        var now = DateTimeOffset.UtcNow;
        return new(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
    }

    public void SetBillingConfigurationContacts(AccountContact primaryAccountContact, bool generatorRepresentativeSameAsBillingContact = false)
    {
        BillingConfiguration.BillingContactId = primaryAccountContact.Id;
        BillingConfiguration.BillingContactName = $"{primaryAccountContact.Name} {primaryAccountContact.LastName}";
        BillingConfiguration.BillingContactAddress = primaryAccountContact.Address;
        BillingConfiguration.GeneratorRepresentativeId = null;
        if (generatorRepresentativeSameAsBillingContact)
        {
            BillingConfiguration.GeneratorRepresentativeId = primaryAccountContact.Id;
        }
    }

    public void SetBillingConfigurationReferences()
    {
        //Hydrate BillingConfiguration references with Accounts
        if (IsAccountTypeThirdPartyAnalytical || IsAccountTypeTruckingCompany)
        {
            BillingConfiguration.Id = default;
        }
        else
        {
            BillingConfiguration.Name = $"{Account.Name} Default";

            //If same account is selected as Customer and Generator
            if (IsAccountTypeCustomer && IsAccountTypeGenerator)
            {
                BillingConfiguration.BillingCustomerAccountId = Account.Id;
                BillingConfiguration.CustomerGeneratorId = Account.Id;
                BillingConfiguration.CustomerGeneratorName = Account.Name;
                var primaryAccountContact = Account.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                   .FirstOrDefault(new AccountContact());

                SetBillingConfigurationContacts(primaryAccountContact, true);
            }
            else if (IsAccountTypeCustomer && !IsAccountTypeGenerator)
            {
                //If current account is Customer; no Generator
                BillingConfiguration.BillingCustomerAccountId = Account.Id;
                BillingConfiguration.CustomerGeneratorId = default;
                BillingConfiguration.CustomerGeneratorName = null;
                var primaryAccountContact = Account.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                   .FirstOrDefault(new AccountContact());

                SetBillingConfigurationContacts(primaryAccountContact);
            }
            else if (IsAccountTypeGenerator && !IsAccountTypeCustomer)
            {
                if (BillingCustomer.Id == default)
                {
                    //Scenario: Main account is Generator and new CustomerAccount created
                    BillingCustomer.Id = Guid.NewGuid();
                }

                BillingConfiguration.BillingCustomerAccountId = BillingCustomer.Id;
                var primaryAccountContact = BillingCustomer.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                           .FirstOrDefault(new AccountContact());

                SetBillingConfigurationContacts(primaryAccountContact);
                foreach (var signatory in BillingConfiguration.Signatories)
                {
                    if (signatory.AccountId == default)
                    {
                        BillingConfiguration.Signatories.First(x => x.Id == signatory.Id).AccountId = BillingCustomer.Id;
                    }
                }

                BillingConfiguration.CustomerGeneratorId = Account.Id;
                BillingConfiguration.CustomerGeneratorName = Account.Name;
                BillingConfiguration.GeneratorRepresentativeId = Account.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                                        .FirstOrDefault(new AccountContact()).Id;

                BillingConfiguration.Name = $"{Account.Name} {BillingCustomer.Name} Default";
                if (!IsCreateBillingCustomer)
                {
                    BillingConfiguration.Name = $"{Account.Name} Default";
                    BillingCustomer.Id = default;
                }
            }
        }
    }

    public void CleanUpAccountReferences()
    {
        if (Account.AccountTypes.Contains(AccountTypesEnum.Customer.ToString()))
        {
            if (Account.BillingType != BillingType.Mail)
            {
                if (Account.AccountAddresses != null && Account.AccountAddresses.Any(x => x.AddressType == AddressType.Mail))
                {
                    Account.MailingRecipientName = string.Empty;
                    if (!Account.AccountAddresses.First(x => x.AddressType == AddressType.Mail).IsPrimaryAddress)
                    {
                        Account.AccountAddresses.Remove(Account.AccountAddresses.First(x => x.AddressType == AddressType.Mail));
                    }
                }
            }
            else
            {
                if (Account.AccountAddresses != null && !Account.AccountAddresses.Any(x => x.IsPrimaryAddress && x.AddressType == AddressType.Mail))
                {
                    Account?.AccountAddresses?.Add(AccountMailingAddress);
                }
            }
        }
        else
        {
            if (BillingCustomer.BillingType != BillingType.Mail)
            {
                if (BillingCustomer.AccountAddresses != null && BillingCustomer.AccountAddresses.Any(x => x.AddressType == AddressType.Mail))
                {
                    BillingCustomer.MailingRecipientName = string.Empty;
                    if (!BillingCustomer.AccountAddresses.First(x => x.AddressType == AddressType.Mail).IsPrimaryAddress)
                    {
                        var mailingAddress = BillingCustomer.AccountAddresses.FirstOrDefault(x => x.AddressType == AddressType.Mail, new());
                        BillingCustomer.AccountAddresses.Remove(mailingAddress);
                    }
                }
            }
            else
            {
                if (BillingCustomer.AccountAddresses != null && !BillingCustomer.AccountAddresses.Any(x => x.IsPrimaryAddress && x.AddressType == AddressType.Mail))
                {
                    BillingCustomer?.AccountAddresses?.Add(AccountMailingAddress);
                }
            }
        }

        if (Account?.AccountAddresses != null && Account.AccountAddresses.Any(x => x.IsPrimaryAddress && x.Province == StateProvince.Unspecified))
        {
            Account.AccountAddresses.Remove(Account.AccountAddresses.First(x => x.IsPrimaryAddress));
        }
    }
}
