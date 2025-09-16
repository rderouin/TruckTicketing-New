using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.AdditionalServicesConfiguration;

public partial class AdditionalServicesConfigurationMatchCriteria : BaseRazorComponent
{
    private bool _disableFacilityServiceSubstanceDropDown;

    private bool _disableSourceIdentifierDropDown;

    private bool _disableWellClassificationDropDown;

    [Parameter]
    public Contracts.Models.Operations.AdditionalServicesConfiguration Model { get; set; }

    [Parameter]
    public AdditionalServicesConfigurationMatchPredicate MatchPredicate { get; set; }

    [Parameter]
    public bool IsNewRecord { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationMatchPredicate> AddMatchPredicate { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationMatchPredicate> UpdateMatchPredicate { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    public IServiceTypeService ServiceTypeService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _disableWellClassificationDropDown = MatchPredicate.WellClassificationState != MatchPredicateValueState.Value;
        _disableSourceIdentifierDropDown = MatchPredicate.SourceIdentifierValueState != MatchPredicateValueState.Value;
        _disableFacilityServiceSubstanceDropDown = MatchPredicate.SubstanceValueState != MatchPredicateValueState.Value;
    }

    private async Task SaveButton_Clicked()
    {
        if (!IsNewRecord)
        {
            await UpdateMatchPredicate.InvokeAsync(MatchPredicate);
        }
        else
        {
            await AddMatchPredicate.InvokeAsync(MatchPredicate);
        }
    }

    private void OnWellClassificationStateChange(MatchPredicateValueState value)
    {
        _disableWellClassificationDropDown = value != MatchPredicateValueState.Value;
    }

    private void OnSourceIdentifierValueStateChange(MatchPredicateValueState value)
    {
        _disableSourceIdentifierDropDown = value != MatchPredicateValueState.Value;
    }

    private void OnFacilityServiceSubstanceValueStateChange(MatchPredicateValueState value)
    {
        _disableFacilityServiceSubstanceDropDown = value != MatchPredicateValueState.Value;
    }

    private void OnSourceLocationIdentifierChange(SourceLocation sourceLocation)
    {
        MatchPredicate.SourceIdentifier = sourceLocation == null ? string.Empty : sourceLocation.Display + " - " + sourceLocation.GeneratorName;
    }

    private void BeforeLoadingFacilityServiceSubstances(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(FacilityServiceSubstanceIndex.FacilityId)] = Model.FacilityId;
    }

    private void OnFacilityServiceSubstanceChange(FacilityServiceSubstanceIndex facilityServiceSubstance)
    {
        MatchPredicate.SubstanceId = facilityServiceSubstance?.Id;
        MatchPredicate.SubstanceName = facilityServiceSubstance?.Substance;
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
}
