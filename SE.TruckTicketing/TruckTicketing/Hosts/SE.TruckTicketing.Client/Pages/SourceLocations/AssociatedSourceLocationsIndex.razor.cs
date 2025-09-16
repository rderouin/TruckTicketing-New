using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.SourceLocations;

public partial class AssociatedSourceLocationsIndex : BaseRazorComponent
{
    private SearchResultsModel<SourceLocation, SearchCriteriaModel> _associatedSourceLocations = new();

    private bool _isLoading;

    private Guid? _selectedAssociatedSourceLocationId;

    private string DropdownPlaceHolder => SourceLocation.SourceLocationTypeCategory == SourceLocationTypeCategory.Well ? "Select a source location to associate..." : string.Empty;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> OnSourceLocationAssociate { get; set; }

    [Parameter]
    public EventCallback<Guid> OnSourceLocationDissociate { get; set; }

    [Parameter]
    public SourceLocation SourceLocation { get; set; }

    private bool IsDissociationDisabled => SourceLocation.SourceLocationTypeCategory != SourceLocationTypeCategory.Well;

    [Inject]
    private IServiceBase<SourceLocation, Guid> SourceLocationService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadAssociatedSourceLocations(new() { PageSize = 10 });
    }

    private async Task DissociateSourceLocation(Guid sourceLocationId)
    {
        _associatedSourceLocations = new();
        await OnSourceLocationDissociate.InvokeAsync(sourceLocationId);
    }

    private async Task LoadAssociatedSourceLocations(SearchCriteriaModel criteria)
    {
        if (SourceLocation.Id == default)
        {
            return;
        }

        //Search Surface Location associated to Wells
        if (SourceLocation?.AssociatedSourceLocationId.GetValueOrDefault(Guid.Empty) != Guid.Empty && !_associatedSourceLocations.Results.Any())
        {
            _isLoading = true;
            var associatedSourceLocation = await SourceLocationService.GetById(SourceLocation.AssociatedSourceLocationId.Value);
            _associatedSourceLocations = new(new[] { associatedSourceLocation });
            _isLoading = false;
            return;
        }

        //Search Wells associated to Surface Location
        criteria?.AddFilterIf(!criteria.Filters.ContainsKey(nameof(SourceLocation.AssociatedSourceLocationId)),
                              nameof(SourceLocation.AssociatedSourceLocationId), SourceLocation.Id);

        _isLoading = true;
        _associatedSourceLocations = await SourceLocationService.Search(criteria);
        _isLoading = false;
    }

    private void SetSurfaceSourceLocationFilter(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(SourceLocation.SourceLocationTypeCategory), SourceLocationTypeCategory.Surface.ToString());
    }

    private async void OnAssociatedSourceLocationSelect(SourceLocation sourceLocation)
    {
        _associatedSourceLocations = new(new[] { sourceLocation });
        await OnSourceLocationAssociate.InvokeAsync(sourceLocation);
        _selectedAssociatedSourceLocationId = null;
    }
}
