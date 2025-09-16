using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Pages;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.ViewModels.BillingConfigurations;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class MatchCriteriaGrid : BaseRazorComponent
{
    private SearchResultsModel<MatchPredicate, SearchCriteriaModel> _matchCriterias = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<MatchPredicate>(),
    };

    private MatchPredicateViewModel _matchPredicateViewModel;

    protected PagableGridView<MatchPredicate> grid;

    private bool isShowDatesEnabled;

    [Parameter]
    public List<(MatchPredicate, BillingConfiguration)> OverlappingMatchPredicates { get; set; }

    [Parameter]
    public BillingConfiguration BillingConfiguration { get; set; }

    [Parameter]
    public InvoiceConfiguration InvoiceConfiguration { get; set; }

    [Parameter]
    public List<SourceLocation> SourceLocations { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    //Events
    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Parameter]
    public EventCallback<MatchPredicate> OnAddEditMatchPredicate { get; set; }

    private async Task AddEditMatchPredicateHandler(MatchPredicate matchPredicate)
    {
        if (matchPredicate.Id == default)
        {
            matchPredicate.Id = Guid.NewGuid();
            BillingConfiguration.MatchCriteria.Add(matchPredicate);
        }
        else
        {
            var updatedMatchPredicate = BillingConfiguration.MatchCriteria.FirstOrDefault(x => x.Id == matchPredicate.Id, new());
            if (matchPredicate.Id != default)
            {
                var index = BillingConfiguration.MatchCriteria.IndexOf(updatedMatchPredicate);
                if (index != -1)
                {
                    BillingConfiguration.MatchCriteria[index] = matchPredicate;
                }
            }
        }

        DialogService.Close();
        await OnAddEditMatchPredicate.InvokeAsync();
        await grid.ReloadGrid();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadMatchCriteria();
    }

    private Task LoadMatchCriteria()
    {
        _matchCriterias = new(BillingConfiguration.MatchCriteria);
        return Task.CompletedTask;
    }

    private async Task EnabledFlagChange(MatchPredicate matchPredicate)
    {
        BillingConfiguration.MatchCriteria.First(x => x.Id == matchPredicate.Id).IsEnabled = matchPredicate.IsEnabled;
        await OnAddEditMatchPredicate.InvokeAsync(matchPredicate);
    }

    private async Task OpenEditDialog(MatchPredicate model = null)
    {
        _matchPredicateViewModel = new(model?.Clone() ?? new MatchPredicate
        {
            SourceLocationValueState = InvoiceConfiguration.IsSplitBySourceLocation ? MatchPredicateValueState.Value : MatchPredicateValueState.NotSet,
            WellClassificationState = InvoiceConfiguration.IsSplitByWellClassification ? MatchPredicateValueState.Value : MatchPredicateValueState.NotSet,
            ServiceTypeValueState = InvoiceConfiguration.IsSplitByServiceType ? MatchPredicateValueState.Value : MatchPredicateValueState.NotSet,
            SubstanceValueState = InvoiceConfiguration.IsSplitBySubstance ? MatchPredicateValueState.Value : MatchPredicateValueState.NotSet,
            StreamValueState = MatchPredicateValueState.NotSet,
        }, BillingConfiguration, InvoiceConfiguration, SourceLocations);

        await DialogService.OpenAsync<MatchCriteriaEdit>(_matchPredicateViewModel.Title,
                                                         new()
                                                         {
                                                             { nameof(MatchCriteriaEdit.ViewModel), _matchPredicateViewModel },
                                                             { nameof(MatchCriteriaEdit.OverlappingMatchPredicates), OverlappingMatchPredicates },
                                                             {
                                                                 nameof(MatchCriteriaEdit.AddEditMatchPredicate),
                                                                 new EventCallback<MatchPredicate>(this, (Func<MatchPredicate, Task>)(async model => await AddEditMatchPredicateHandler(model)))
                                                             },
                                                             { nameof(MatchCriteriaEdit.OnCancel), HandleCancel },
                                                         },
                                                         new()
                                                         {
                                                             Width = "40%",
                                                         });
    }

    private async Task DeleteButton_Click(MatchPredicate model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Match Predicate";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            BillingConfiguration.MatchCriteria
                                .Remove(BillingConfiguration.MatchCriteria.First(x => x.Id ==
                                                                                      model.Id));

            await OnAddEditMatchPredicate.InvokeAsync(model);
            await grid.ReloadGrid();
        }
    }

    private Task EnableDisableShowDates()
    {
        isShowDatesEnabled = !isShowDatesEnabled;
        return Task.CompletedTask;
    }
}
