using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Pages.AdditionalServicesConfiguration;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.AdditionalServicesConfigurationComponents;

public partial class AdditionalServicesConfigurationMatchCriteriaGrid : BaseRazorComponent
{
    private SearchResultsModel<AdditionalServicesConfigurationMatchPredicate, SearchCriteriaModel> _matchCriterias = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<AdditionalServicesConfigurationMatchPredicate>(),
    };

    [Parameter]
    public AdditionalServicesConfiguration Model { get; set; }

    [Parameter]
    public List<AdditionalServicesConfigurationMatchPredicate> MatchPredicatesList { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationMatchPredicate> OnMatchPredicateChange { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationMatchPredicate> OnMatchPredicateAdd { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationMatchPredicate> OnMatchPredicateDeleted { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadMatchCriteria();
    }

    private Task LoadMatchCriteria()
    {
        _matchCriterias = new(MatchPredicatesList);
        return Task.CompletedTask;
    }

    private Task NavigateEditPage(AdditionalServicesConfigurationMatchPredicate matchPredicate)
    {
        NavigationManager.NavigateTo($"/MatchPredicate/{matchPredicate.Id}");
        return Task.CompletedTask;
    }

    private async Task EnabledFlagChange(AdditionalServicesConfigurationMatchPredicate matchPredicate)
    {
        await OnMatchPredicateChange.InvokeAsync(matchPredicate);
    }

    private async Task AddAdditionalServicesConfigurationMatchPredicate(AdditionalServicesConfigurationMatchPredicate matchPredicate)
    {
        DialogService.Close();
        await OnMatchPredicateAdd.InvokeAsync(matchPredicate);
    }

    private async Task UpdateAdditionalServicesConfigurationMatchPredicate(AdditionalServicesConfigurationMatchPredicate matchPredicate)
    {
        DialogService.Close();
        await OnMatchPredicateChange.InvokeAsync(matchPredicate);
    }

    private async Task OpenEditDialog(AdditionalServicesConfigurationMatchPredicate matchPredicate, bool isNew)
    {
        await DialogService.OpenAsync<AdditionalServicesConfigurationMatchCriteria>("Match Predicate",
                                                                                    new()
                                                                                    {
                                                                                        { "Model", Model },
                                                                                        { "MatchPredicate", matchPredicate },
                                                                                        { "IsNewRecord", isNew },
                                                                                        {
                                                                                            nameof(AdditionalServicesConfigurationMatchCriteria.AddMatchPredicate),
                                                                                            new EventCallback<AdditionalServicesConfigurationMatchPredicate>(this,
                                                                                                AddAdditionalServicesConfigurationMatchPredicate)
                                                                                        },
                                                                                        {
                                                                                            nameof(AdditionalServicesConfigurationMatchCriteria.UpdateMatchPredicate),
                                                                                            new EventCallback<AdditionalServicesConfigurationMatchPredicate>(this,
                                                                                                UpdateAdditionalServicesConfigurationMatchPredicate)
                                                                                        },
                                                                                        { nameof(AdditionalServicesConfigurationMatchCriteria.OnCancel), HandleCancel },
                                                                                    });
    }

    private async Task DeleteButton_Click(AdditionalServicesConfigurationMatchPredicate matchPredicate)
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
            await OnMatchPredicateDeleted.InvokeAsync(matchPredicate);
        }
    }
}
