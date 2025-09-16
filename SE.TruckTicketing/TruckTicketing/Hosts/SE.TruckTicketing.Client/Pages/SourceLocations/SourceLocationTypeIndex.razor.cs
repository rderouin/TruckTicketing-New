using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.SourceLocations;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.SourceLocations;

public partial class SourceLocationTypeIndex : BaseTruckTicketingComponent
{
    private SourceLocationTypeDetailsViewModel _detailsViewModel;

    private PagableGridView<SourceLocationType> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<SourceLocationType, SearchCriteriaModel> _resultsModel = new();

    [Inject]
    protected NotificationService NotificationService { get; set; }

    [Inject]
    protected IServiceBase<SourceLocationType, Guid> SourceLocationTypeService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<SourceLocationType> HandleValidSubmit => new(this, OnSubmit);

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task OnSubmit(SourceLocationType model)
    {
        var response = await SourceLocationTypeService.Create(model);

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success,
                                       $"{model.Name} source location type {(model.Id == default ? "created" : "updated")}.");

            DialogService.Close();

            await _grid.ReloadGrid();
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to save source location type.");
        }

        _detailsViewModel.Response = response;
    }

    protected async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        _resultsModel = await SourceLocationTypeService.Search(criteria) ?? _resultsModel;
        _isLoading = false;

        StateHasChanged();
    }

    protected async Task OpenSourceLocationTypeDetailsDialog(SourceLocationType model = null)
    {
        _detailsViewModel = new(model?.Clone() ?? new SourceLocationType());

        await DialogService.OpenAsync<SourceLocationTypeDetails>(_detailsViewModel.Title,
                                                                 new()
                                                                 {
                                                                     { nameof(SourceLocationTypeDetails.ViewModel), _detailsViewModel },
                                                                     { nameof(SourceLocationTypeDetails.OnCancel), HandleCancel },
                                                                     { nameof(SourceLocationTypeDetails.OnSubmit), HandleValidSubmit },
                                                                 });
    }
}
