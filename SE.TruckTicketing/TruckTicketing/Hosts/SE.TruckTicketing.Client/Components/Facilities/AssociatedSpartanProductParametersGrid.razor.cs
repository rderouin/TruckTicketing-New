using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Models.FacilityServices;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.Facilities;

public partial class AssociatedSpartanProductParametersGrid : BaseTruckTicketingComponent
{
    private SearchResultsModel<FacilityServiceSpartanProductParameter, SearchCriteriaModel> _associatedSpartanProductParameters = new();

    private PagableGridView<FacilityServiceSpartanProductParameter> _grid;

    private Guid? _selectedSpartanProductParameter;

    [Parameter]
    public FacilityService FacilityService { get; set; }

    [Parameter]
    public Guid LegalEntityId { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadData(null);
    }

    private void BeforeLoadingSpartanProductParameters(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(SpartanProductParameter.IsDeleted)] = false;
        criteria.Filters[nameof(SpartanProductParameter.IsActive)] = true;
        criteria.Filters[nameof(SpartanProductParameter.LegalEntityId)] = LegalEntityId;
    }

    private async void HandleSpartanProductParameterSelection(SpartanProductParameter parameter)
    {
        FacilityService.SpartanProductParameters ??= new();

        if (FacilityService.SpartanProductParameters.Any(p => p.SpartanProductParameterId == parameter.Id))
        {
            return;
        }

        FacilityService.SpartanProductParameters.Add(new()
        {
            SpartanProductParameterId = parameter.Id,
            SpartanProductParameterDisplay = parameter.Display,
        });

        _selectedSpartanProductParameter = null;
        await _grid.ReloadGrid();
    }

    private async Task HandleSpartanProductParameterDelete(FacilityServiceSpartanProductParameter parameter)
    {
        FacilityService.SpartanProductParameters = FacilityService.SpartanProductParameters
                                                                  .Where(p => p.SpartanProductParameterId != parameter.SpartanProductParameterId)
                                                                  .ToList();

        await _grid.ReloadGrid();
    }

    private void LoadData(SearchCriteriaModel _)
    {
        _associatedSpartanProductParameters = new(FacilityService.SpartanProductParameters);
        StateHasChanged();
    }
}
