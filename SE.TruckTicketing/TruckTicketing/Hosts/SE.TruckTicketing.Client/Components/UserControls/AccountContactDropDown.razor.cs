using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class AccountContactDropDown<TValue> : TridentApiDropDownDataGrid<AccountContactIndex, TValue>
{
    [Parameter]
    public Guid? AccountId { get; set; }

    [Parameter]
    public AccountContactFunctions ContactFunction { get; set; }

    protected override async Task OnLoadData(LoadDataArgs args)
    {
        if (AccountId.GetValueOrDefault(Guid.Empty) == Guid.Empty)
        {
            return;
        }

        await base.OnLoadData(args);
        StateHasChanged();
    }

    protected override Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(AccountContactIndex.AccountId)] = AccountId;
        criteria.Filters[nameof(AccountContactIndex.IsActive)] = true;
        criteria.Filters[nameof(AccountContactIndex.ContactFunctions).AsPrimitiveCollectionFilterKey()] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = ContactFunction.ToString(),
        };

        criteria.OrderBy = nameof(AccountContactIndex.Name);
        criteria.SortOrder = SortOrder.Asc;

        return base.BeforeDataLoad(criteria);
    }

    public async Task Reload(Guid accountId)
    {
        AccountId = accountId;
        await OnLoadData(new() { Top = PageSize == 0 ? 10 : PageSize });
        StateHasChanged();
    }
}
