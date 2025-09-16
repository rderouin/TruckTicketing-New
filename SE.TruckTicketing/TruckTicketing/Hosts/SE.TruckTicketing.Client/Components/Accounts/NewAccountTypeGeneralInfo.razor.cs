using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class NewAccountTypeGeneralInfo : BaseRazorComponent
{
    protected RadzenTemplateForm<Account> ReferenceToAccountForm;

    [Parameter]
    public Account account { get; set; } = new();

    [Parameter]
    public IEnumerable<AccountTypes> SelectedAccountTypes { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<AccountTypes>> AccountTypeSelectionChange { get; set; }

    [Parameter]
    public EventCallback<CountryCode> LegalEntityCountryCodeChange { get; set; }

    [CascadingParameter]
    private string AccountType { get; set; }

    protected void HandleLegalEntityLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(LegalEntity.Name);
        criteria.Filters[nameof(LegalEntity.ShowAccountsInTruckTicketing)] = true;
    }

    public async Task OnLegalEntitySelect(LegalEntity legalEntity)
    {
        account.LegalEntity = legalEntity.Code;
        account.IsShowAccount = legalEntity.ShowAccountsInTruckTicketing ?? false;
        if (LegalEntityCountryCodeChange.HasDelegate)
        {
            await LegalEntityCountryCodeChange.InvokeAsync(legalEntity.CountryCode);
        }
    }

    public async Task OnAccountTypeChange(IEnumerable<AccountTypes> selectedAccountTypes)
    {
        await AccountTypeSelectionChange.InvokeAsync(selectedAccountTypes);
    }
}
