using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;

using CompareOperators = Trident.Search.CompareOperators;

using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Search;

namespace SE.TruckTicketing.Client.Security;

public partial class PermissionFilter : FilterComponent<IEnumerable<Guid>>
{
    public const string PermissionType = nameof(PermissionType);

    [Parameter]
    public Trident.Search.CompareOperators CompareOperator { get; set; } = CompareOperators.eq;

    private List<ListOption<Guid>> Data { get; set; }

    [Inject]
    private IRoleService RoleService { get; set; }

    private IEnumerable<Guid> SelectedValues { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData(new());
            StateHasChanged();
        }
    }

    private async Task LoadData(LoadDataArgs args)
    {
        var criteria = args.ToSearchCriteriaModel();

        var permissionList = await RoleService.GetPermissionList();
        Data = Data ?? new();
        permissionList.ForEach(x => x.AllowedOperations.ToList().ForEach(allowedOp => Data.Add(new ListOption<Guid>()
        {
            Value = allowedOp.Id,
            Display = $"{allowedOp.Display} {x.Display}"
        })));
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        var values = SelectedValues?.ToArray() ?? Array.Empty<Guid>();

        if (!values.Any())
        {
            criteria?.Filters?.Remove(PermissionType);
            criteria.PageSize = 10;
        }
        else
        {
            criteria.Filters[PermissionType] = values;
            criteria.PageSize = int.MaxValue;
        }
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        SelectedValues = Array.Empty<Guid>();
        criteria?.Filters?.Remove(PermissionType);
    }

    private async Task HandleChange()
    {
        await PropagateFilterValueChange(SelectedValues?.ToArray() ?? Enumerable.Empty<Guid>());
    }
}
