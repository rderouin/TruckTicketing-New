using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.Accounts;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class AccountsDropDown<TValue> : TridentApiDropDownDataGrid<Account, TValue>
{
    [Parameter]
    public string AccountType { get; set; }

    [Parameter]
    public bool ApplyShowAccountFilter { get; set; } = true;

    protected override async Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        if (string.IsNullOrWhiteSpace(AccountType))
        {
            return;
        }

        criteria.Filters[nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountType,
        };

        criteria.Filters[nameof(Account.IsAccountActive)] = true;
        if (ApplyShowAccountFilter)
        {
            criteria.Filters[nameof(Account.IsShowAccount)] = true;
        }

        await base.BeforeDataLoad(criteria);
    }

    public async Task CreateNewAccount()
    {
        await DialogService.OpenAsync<NewAccountDialog>($"New - {AccountType.Replace("ThirdPartyAnalytical", "3rd Party Analytical", StringComparison.OrdinalIgnoreCase)}", new()
        {
            { "AccountType", AccountType },
            { "AddAccount", new EventCallback<Account>(this, NewAccountAdded) },
        }, new()
        {
            Width = "80%",
            Height = "80%",
        });
    }

    private async Task NewAccountAdded(Account account)
    {
        Value = (TValue)Convert.ChangeType(account.Id, typeof(TValue));
        await Reload();
        await base.InvokeOnItemSelect(account);
        StateHasChanged();
    }
}
