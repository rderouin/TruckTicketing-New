using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Configuration;
using Trident.Security;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.Accounts;

public partial class AccountsIndexPage : BaseTruckTicketingComponent
{
    private const string EditBasePath = "/account/edit";

    private readonly List<ListOption<bool>> _activeAccountListBoxData = new()
    {
        new()
        {
            Value = true,
            Display = "Active",
        },
        new()
        {
            Value = false,
            Display = "In-Active",
        },
    };

    private SearchResultsModel<Account, SearchCriteriaModel> _accounts = new();

    private PagableGridView<Account> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    //Services
    [Inject]
    public IAppSettings AppSettings { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }
    
    [Inject]
    private IServiceBase<BusinessStream, Guid> BusinessStreamService { get; set; }

    private bool HasAccountWritePermission => HasWritePermission(Permissions.Resources.Account);

    private string AddAccountLink_Css => GetLink_CssClass(HasAccountWritePermission);

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task LoadData(SearchCriteriaModel current)
    {
        _isLoading = true;
        current.Filters[nameof(Account.IsShowAccount)] = true;
        _accounts = await AccountService.Search(current) ?? _accounts;
        _isLoading = false;
        StateHasChanged();
    }

    private async Task EditButton_Click(Account model)
    {
        await NavigateEditPage(model);
    }

    private Task NavigateEditPage(Account model)
    {
        NavigationManager.NavigateTo($"{EditBasePath}/{model.Id}");
        return Task.CompletedTask;
    }

   
}
