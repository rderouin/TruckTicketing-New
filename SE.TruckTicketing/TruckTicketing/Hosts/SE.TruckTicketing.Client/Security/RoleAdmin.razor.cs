using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Accounts;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Mapper;

using SortOrder = Trident.Contracts.Enums.SortOrder;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Client.Components;

namespace SE.TruckTicketing.Client.Security;

public partial class RoleAdmin : BaseTruckTicketingComponent
{
    private SearchResultsModel<Role, SearchCriteriaModel> _results = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<Role>(),
    };

    private GridFiltersContainer _gridFiltersContainer;

    [Inject]
    private IRoleService RoleService { get; set; }

    [Inject]
    private IMapperRegistry Mapper { get; set; }

    [Parameter]
    public bool ReadOnlyUser { get; set; }

    protected override bool ShouldRender()
    {
        return true;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            DialogService.OnClose += Close;
            ReadOnlyUser = false;
        }
        catch (Exception e)
        {
            HandleException(e, nameof(RoleAdmin), "An exception occurred getting claims in OnInitializedAsync");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await PerformRoleSearchAsync();
    }

    private async Task LoadData(SearchCriteriaModel current)
    {
        //load results from server
        _results = await RoleService.Search(current) ?? _results;

        if (current.Filters.TryGetValue("PermissionType", out var selectedValues) && selectedValues is IEnumerable<Guid> selectedIds)
        {
            var ids = selectedIds.ToList();
            var filteredRoles = _results.Results.Where(role => role.Permissions.Any(permission => permission.AllowedOperations.Any(ops => ids.Contains(ops.Id))));
            _results.Results = filteredRoles;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenEditDialog(Role model)
    {
        var viewModel = Mapper.Map<RoleViewModel>(model);

        viewModel.SubmitButtonDisabled = !HasWritePermission(Permissions.Resources.Roles);
        await DialogService.OpenAsync<RoleEdit>("User Role",
                                                new() { { "Role", viewModel } },
                                                new()
                                                {
                                                    Width = "80%",
                                                });

        await LoadData(_results.Info);
    }

    private async Task AddButton_Click()
    {
        await OpenEditDialog(new());
    }

    private async Task EditButton_Click(Role model)
    {
        await OpenEditDialog(model);
    }

    private async Task DeleteButton_Click(Role model)
    {
        const string msg = "Are you sure you want to delete this item?";
        const string title = "Delete Role";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Yes",
                                                              CancelButtonText = "No",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            var result = await RoleService.Delete(model);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                var resultList = _results.Results.ToList();
                var index = resultList.RemoveAll(x => x.Id == model.Id);
                _results.Results = resultList;
                _results.Info.TotalRecords--;
            }
        }
    }

    public override void Dispose()
    {
        DialogService.OnClose -= Close;
    }

    private void Close(dynamic result)
    {
        if (result is not Role role)
        {
            return;
        }

        var resultList = _results.Results.ToList();
        var index = resultList.FindIndex(x => x.Id == role.Id);
        if (index >= 0)
        {
            resultList[index] = role;
        }
        else
        {
            resultList.Add(role);
            _results.Info.TotalRecords++;
        }

        _results.Results = resultList;
    }

    public async Task PerformRoleSearchAsync()
    {
        var criteria = new SearchCriteriaModel
        {
            PageSize = 10,
            CurrentPage = 0,
            Keywords = "",
            OrderBy = "",
            Filters = new() { { "EntityType", "Role" } },
            SortOrder = SortOrder.Asc,
        };

        _results = await RoleService.Search(criteria) ?? _results;
        await InvokeAsync(StateHasChanged);
    }
}
