using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.UI.ViewModels.Accounts;

public class AccountContactViewModel
{
    private readonly Dictionary<AccountContactFunctions, string> AccountContactFunctionDataByCategory = new();

    private readonly List<ListOption<string>> contactFunctionForAccountType = new();

    public string PostalCodeMask = "*****-****";

    public string PostalCodePattern = "[0-9]";

    public string PrimaryAccountContactValidationMessage = "Primary billing contact should have 'Billing Contact' as contact function";

    public Dictionary<StateProvince, string> StateProviceData = new();

    public string StateProvinceLabel = "State/Province";

    public string StateProvincePlaceHolder = "Select State/Province...";

    public string ZipPostalCodeLabel = "Zip/Postal Code";

    public AccountContactViewModel(AccountContact accountContact, Account account)
    {
        AccountContact = accountContact;
        IsNew = AccountContact?.Id == Guid.Empty;
        Breadcrumb = IsNew ? "New Account Contact" : "Edit Account Contact";
        IsCustomerAccountType = account.AccountTypes.Contains(AccountTypes.Customer.ToString());
        CurrentAccount = account;
        if (!IsNew || (AccountContact?.AccountContactAddress != null && AccountContact?.AccountContactAddress.Country != CountryCode.Undefined))
        {
            UpdateStateProvinceLabel(accountContact.AccountContactAddress.Country.ToString());
            if(AccountContact?.AccountContactAddress.Country != CountryCode.Undefined)
            {
                StateProviceData = StateProvinceDataByCategory[accountContact.AccountContactAddress.Country.ToString()];
            }
        }

        foreach (var accountType in account.AccountTypes)
        {
            ContactFunctionsDataByCategory.TryGetValue(accountType, out AccountContactFunctionDataByCategory);
            if (AccountContactFunctionDataByCategory != null)
            {
                contactFunctionForAccountType = AccountContactFunctionDataByCategory.Select(x =>
                                                                                                new ListOption<string>
                                                                                                {
                                                                                                    Display = x.Value,
                                                                                                    Value = x.Value,
                                                                                                }).ToList();

                ContactFunctionsListData.AddRange(contactFunctionForAccountType);
            }
        }

        contactFunctionForAccountType = ContactFunctionsDataByCategory["All"].Select(x =>
                                                                                         new ListOption<string>
                                                                                         {
                                                                                             Display = x.Value,
                                                                                             Value = x.Value,
                                                                                         }).ToList();

        ContactFunctionsListData.AddRange(contactFunctionForAccountType);
    }

    private string ContactFormTitleWithAccountName => string.IsNullOrEmpty(CurrentAccount.Name) ? string.Empty : $"for {CurrentAccount.Name} Account";

    public Account CurrentAccount { get; set; } = new();

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public bool IsCustomerAccountType { get; set; }

    public AccountContact AccountContact { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public string Title => IsNew ? $"Create Contact {ContactFormTitleWithAccountName}" : $"Edit Contact {ContactFormTitleWithAccountName}";

    public string SubmitButtonText => IsNew ? "Create" : "Update";

    public string PrimaryContactSwitchLabel => IsCustomerAccountType ? "Default Billing Contact" : "Primary Contact";

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public List<ListOption<string>> ContactFunctionsListData { get; } = new();

    public IDictionary<string, Dictionary<StateProvince, string>> StateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    public IDictionary<string, Dictionary<AccountContactFunctions, string>> ContactFunctionsDataByCategory => DataDictionary.GetDataDictionaryByCategory<AccountContactFunctions>();

    public CountryCode SelectedCountry
    {
        get => AccountContact.AccountContactAddress.Country;
        set
        {
            AccountContact.AccountContactAddress.Country = value;
            UpdateStateProvinceLabel(value.ToString());
            StateProviceData = StateProvinceDataByCategory[Enum.Parse<CountryCode>(value.ToString()).ToString()];
        }
    }

    public IEnumerable<string> ContactFunctions
    {
        get =>
            AccountContact.ContactFunctions.Select(x =>
                                                   {
                                                       return x.GetEnumDescription<AccountContactFunctions>();
                                                   }).ToList();
        set =>
            AccountContact.ContactFunctions = value.Select(x =>
                                                           {
                                                               return x.GetEnumValue<AccountContactFunctions>();
                                                           }).ToList();
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
