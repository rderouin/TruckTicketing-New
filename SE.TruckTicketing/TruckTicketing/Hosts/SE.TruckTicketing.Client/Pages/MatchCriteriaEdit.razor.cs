using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Models.Substances;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.BillingConfigurations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Pages;

public partial class MatchCriteriaEdit : BaseRazorComponent
{
    private List<string> billingConfigurationsWithDuplicateMatchPredicate = new();

    private bool disableServiceTypeDropDown;

    //Enable Dropdown
    private bool disableSourceLocationDropDown;

    private bool disableStreamDropDown;

    private bool disableSubstanceDropDown;

    private bool disableWellClassificationDropDown;

    private bool isDuplicate;

    private RadzenTemplateForm<MatchPredicate> ReferenceToForm;

    [Parameter]
    public MatchPredicateViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback<MatchPredicate> AddEditMatchPredicate { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public bool IsNewRecord { get; set; }

    [Parameter]
    public List<(MatchPredicate, BillingConfiguration)> OverlappingMatchPredicates { get; set; }

    [Inject]
    private IServiceBase<BillingConfiguration, Guid> BillingConfigurationService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        disableSourceLocationDropDown = ViewModel.MatchPredicate.SourceLocationValueState != MatchPredicateValueState.Value;
        disableWellClassificationDropDown = ViewModel.MatchPredicate.WellClassificationState != MatchPredicateValueState.Value;
        disableStreamDropDown = ViewModel.MatchPredicate.StreamValueState != MatchPredicateValueState.Value;
        disableSubstanceDropDown = ViewModel.MatchPredicate.SubstanceValueState != MatchPredicateValueState.Value;
        disableServiceTypeDropDown = ViewModel.MatchPredicate.ServiceTypeValueState != MatchPredicateValueState.Value;
    }

    private async Task SaveButton_Clicked()
    {
        //Check for Duplicates
        billingConfigurationsWithDuplicateMatchPredicate = new();
        if (ViewModel.MatchPredicate.IsEnabled)
        {
            ViewModel.MatchPredicate.ComputeHash();
        }

        isDuplicate = IsDuplicate();
        if (!isDuplicate)
        {
            await AddEditMatchPredicate.InvokeAsync(ViewModel.MatchPredicate);
        }
        else
        {
            var billingConfigs = OverlappingMatchPredicates.Where(x => x.Item1.Hash == ViewModel.MatchPredicate.Hash).ToList();
            if (billingConfigs.Count > 0)
            {
                billingConfigs.ForEach(x =>
                                       {
                                           if (!billingConfigurationsWithDuplicateMatchPredicate.Contains(x.Item2.Name))
                                           {
                                               billingConfigurationsWithDuplicateMatchPredicate.Add(x.Item2.Name);
                                           }
                                       });
            }
            else
            {
                if (!billingConfigurationsWithDuplicateMatchPredicate.Contains(ViewModel.BillingConfiguration.Name))
                {
                    billingConfigurationsWithDuplicateMatchPredicate.Add(ViewModel.BillingConfiguration.Name);
                }
            }
        }
    }

    private bool IsDuplicate()
    {
        var duplicateCheckForMatchingBillingConfigurations = false;
        var duplicateCheckForCurrentBillingConfiguration = false;
        var startDate = ViewModel.MatchPredicate.StartDate ?? ViewModel.BillingConfiguration.StartDate ?? DateTimeOffset.MinValue;
        var endDate = ViewModel.MatchPredicate.EndDate ?? ViewModel.BillingConfiguration.EndDate ?? DateTimeOffset.MaxValue;
        if (ViewModel.BillingConfiguration.MatchCriteria != null && ViewModel.BillingConfiguration.MatchCriteria.Any())
        {
            var validMatchCriteriaOnCurrentBillingConfiguration = ViewModel.BillingConfiguration.MatchCriteria.OrderBy(x => x.StartDate).ToList();
            validMatchCriteriaOnCurrentBillingConfiguration = validMatchCriteriaOnCurrentBillingConfiguration.Where(criteria => (criteria.StartDate == null || criteria.StartDate < endDate) &&
                                                                                                                                (criteria.EndDate == null || criteria.EndDate >= startDate)
                                                                                                                             && criteria.IsEnabled).ToList();

            duplicateCheckForCurrentBillingConfiguration = ViewModel.IsNew
                                                               ? validMatchCriteriaOnCurrentBillingConfiguration.Any(x => x.Hash == ViewModel.MatchPredicate.Hash)
                                                               : validMatchCriteriaOnCurrentBillingConfiguration.Any(x => x.Hash == ViewModel.MatchPredicate.Hash &&
                                                                                                                          x.Id != ViewModel.MatchPredicate.Id);
        }

        duplicateCheckForMatchingBillingConfigurations = OverlappingMatchPredicates.Any(x => x.Item1.Hash == ViewModel.MatchPredicate.Hash);

        return duplicateCheckForMatchingBillingConfigurations || duplicateCheckForCurrentBillingConfiguration;
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    //Handle DropDown Value Change
    private void OnSourceLocationIdentifierChange(SourceLocation sourceLocation)
    {
        ViewModel.MatchPredicate.SourceIdentifier = sourceLocation.Display;
    }

    private void OnSubstanceChange(Substance substance)
    {
        ViewModel.MatchPredicate.SubstanceName = substance.SubstanceName;
    }

    private void OnServiceTypeChange(ServiceType serviceType)
    {
        ViewModel.MatchPredicate.ServiceType = serviceType.Name;
    }

    //Handle CheckBoxList change events
    private void OnSourceLocationValueStateChange(MatchPredicateValueState value)
    {
        disableSourceLocationDropDown = value != MatchPredicateValueState.Value;
        if (disableSourceLocationDropDown)
        {
            ViewModel.MatchPredicate.SourceLocationId = null;
            ViewModel.MatchPredicate.SourceIdentifier = null;
        }
    }

    private void OnWellClassificationStateChange(MatchPredicateValueState value)
    {
        disableWellClassificationDropDown = value != MatchPredicateValueState.Value;
        if (disableWellClassificationDropDown)
        {
            ViewModel.MatchPredicate.WellClassification = WellClassifications.Undefined;
        }
    }

    private void OnStreamStateChange(MatchPredicateValueState value)
    {
        disableStreamDropDown = value != MatchPredicateValueState.Value;
        if (disableStreamDropDown)
        {
            ViewModel.MatchPredicate.Stream = Stream.Undefined;
        }
    }

    private void OnSubstanceStateChange(MatchPredicateValueState value)
    {
        disableSubstanceDropDown = value != MatchPredicateValueState.Value;
        if (disableSubstanceDropDown)
        {
            ViewModel.MatchPredicate.SubstanceId = null;
            ViewModel.MatchPredicate.SubstanceName = null;
        }
    }

    private void OnServiceTypeValueStateChange(MatchPredicateValueState value)
    {
        disableServiceTypeDropDown = value != MatchPredicateValueState.Value;
        if (disableServiceTypeDropDown)
        {
            ViewModel.MatchPredicate.ServiceTypeId = null;
            ViewModel.MatchPredicate.ServiceType = null;
        }
    }

    private void BeforeSourceLocationLoad(SearchCriteriaModel criteria)
    {
        List<Guid> sourceLocationData = new();
        if (ViewModel.GeneratorSourceLocationData != null && ViewModel.GeneratorSourceLocationData.Any())
        {
            sourceLocationData = ViewModel.GeneratorSourceLocationData.Select(x => x.Id).ToList();
        }
        else
        {
            sourceLocationData.Add(Guid.Empty);
        }

        if (!ViewModel.InvoiceConfiguration.AllSourceLocations)
        {
            ViewModel.ApplyFilter(criteria, sourceLocationData, nameof(SourceLocation.SearchableId), CompareOperators.eq);
        }

        if (ViewModel.InvoiceConfiguration.AllSourceLocations && ViewModel.BillingConfiguration.CustomerGeneratorId != Guid.Empty)
        {
            criteria.Filters[nameof(SourceLocation.GeneratorId)] = ViewModel.BillingConfiguration.CustomerGeneratorId;
        }
    }

    private void BeforeSubstanceLoad(SearchCriteriaModel criteria)
    {
        if (ViewModel.InvoiceConfiguration.IsSplitBySubstance && ViewModel.InvoiceConfiguration.Substances != null && ViewModel.InvoiceConfiguration.Substances.Any())
        {
            ViewModel.ApplyFilter(criteria, ViewModel.InvoiceConfiguration.Substances, nameof(Substance.SearchableId), CompareOperators.eq);
        }
    }

    private void BeforeServiceTypeLoad(SearchCriteriaModel criteria)
    {
        if (ViewModel.InvoiceConfiguration.IsSplitByServiceType && ViewModel.InvoiceConfiguration.ServiceTypes != null && ViewModel.InvoiceConfiguration.ServiceTypes.Any())
        {
            ViewModel.ApplyFilter(criteria, ViewModel.InvoiceConfiguration.ServiceTypes, nameof(ServiceType.SearchableId), CompareOperators.eq);
        }

        if (ViewModel.InvoiceConfiguration.CustomerLegalEntityId != Guid.Empty)
        {
            criteria.Filters[nameof(ServiceType.LegalEntityId)] = ViewModel.InvoiceConfiguration.CustomerLegalEntityId;
        }
    }
}
