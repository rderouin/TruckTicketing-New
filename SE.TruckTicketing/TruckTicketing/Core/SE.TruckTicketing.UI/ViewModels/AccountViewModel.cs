using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;

using AccountTypesEnum = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.UI.ViewModels;

public class AccountViewModel
{
    public IEnumerable<string> SelectedAccountTypes = new List<string>();

    public AccountViewModel(Account account)
    {
        Account = account;
        Breadcrumb = IsNew ? "New Account" : "Edit Account";
        Title = IsNew ? "Creating Account" : $"Editing {account?.Name} Account";
        IsNew = Account.Id == default;

        SelectedAccountTypes = account.AccountTypes.Select(x =>
                                                           {
                                                               return x.GetEnumDescription<AccountTypesEnum>();
                                                           }).ToList();

        AccountTypeListData = DataDictionary.For<AccountTypesEnum>().Select(x =>
                                                                                new ListOption<string>
                                                                                {
                                                                                    Display = x.Value,
                                                                                    Value = x.Value,
                                                                                }).ToList();
    }

    public CountryCode LegalEntityCountryCode { get; set; }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public Account Account { get; set; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create" : "Save";

    public string SubmitSuccessNotificationMessage => IsNew ? "Account created." : "Account updated.";

    public string Title { get; }

    public bool IsAccountBillable => !IsNew && Account.AccountTypes.Contains(AccountTypes.Customer);

    public bool IsAccountTypeCustomer => !IsNew && Account.AccountTypes.Contains(AccountTypes.Customer);

    public bool IsAccountTypeGenerator => Account.AccountTypes.Contains(AccountTypes.Generator);

    public bool IsAccountTypeTruckingCompany => !IsNew && Account.AccountTypes.Count == 1 && Account.AccountTypes.Contains(AccountTypes.TruckingCompany);

    public List<ListOption<string>> AccountTypeListData { get; } = new();

    public LegalEntity LegalEntity { get; set; }
}

public class ListOption<T>
{
    public T Value { get; set; }

    public string Display { get; set; }
}
