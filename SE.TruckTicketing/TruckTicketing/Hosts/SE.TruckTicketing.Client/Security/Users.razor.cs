using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Security;

public partial class Users : BaseRazorComponent
{
    private GridFiltersContainer _gridFiltersContainer;

    private bool _isLoading;

    private SearchResultsModel<UserProfile, SearchCriteriaModel> _results = new();

    [Inject]
    private IUserProfileService UserProfileService { get; set; }

    [Parameter]
    public bool ReadOnlyUser { get; set; }

    private async Task LoadData(SearchCriteriaModel current)
    {
        _isLoading = true;
        StateHasChanged();
        _results = await UserProfileService.Search(current) ?? _results;
        _isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFiltersContainer.Reload();
        }
    }
}
