using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.ViewModels.EDIFieldDefinitions;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.BillingControls;

public partial class EDIFieldsDefinition
{
    private PagableGridView<EDIFieldDefinition> _grid;

    private bool _isLoading;

    private SearchResultsModel<EDIFieldDefinition, SearchCriteriaModel> _searchResults = new();

    [Parameter]
    public Account Model { get; set; }


    [Parameter]
    public bool Disabled
    {
        get
        {
            return !IsAuthorizedFor(Permissions.Resources.AccountEdiFieldDefinition, Permissions.Operations.Write) || !(Model.IsEdiFieldsEnabled);
        }
        set { }
    }

    [Parameter]
    public EventCallback<EDIFieldDefinition> OnEDIFieldDefinitionAdd { get; set; }

    [Parameter]
    public EventCallback<EDIFieldDefinition> OnEDIFieldDefinitionChange { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceProxyBase<EDIFieldDefinition, Guid> EDIFieldDefinitionService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    protected override async Task OnInitializedAsync()
    {
        await LoadData(new()
        {
            PageSize = 10,
            Filters = new()
            {
                { nameof(EDIFieldDefinition.CustomerId), Model.Id },
            },
        });
    }

    private async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        _searchResults = await EDIFieldDefinitionService.Search(criteria) ?? _searchResults;
        _isLoading = false;
    }

    private async Task AddEDIFieldDefinition(EDIFieldDefinition model)
    {
        DialogService.Close();
        await OnEDIFieldDefinitionAdd.InvokeAsync(model);
        await _grid.ReloadGrid();
    }

    private async Task UpdateEDIFieldDefinition(EDIFieldDefinition model)
    {
        DialogService.Close();
        await OnEDIFieldDefinitionChange.InvokeAsync(model);
    }

    private async Task OpenEditDialog(EDIFieldDefinition model, bool isNew)
    {
        model.CustomerId = Model.Id;
        var viewModel = new EDIFieldDefinitionDetailsViewModel(model);
        await DialogService.OpenAsync<EDIFieldDefinitionEdit>("EDI Field Definition",
                                                              new()
                                                              {
                                                                  { "ViewModel", viewModel },
                                                                  { "IsNewRecord", isNew },
                                                                  { nameof(EDIFieldDefinitionEdit.AddEDIFieldDefinition), new EventCallback<EDIFieldDefinition>(this, AddEDIFieldDefinition) },
                                                                  { nameof(EDIFieldDefinitionEdit.UpdateEDIFieldDefinition), new EventCallback<EDIFieldDefinition>(this, UpdateEDIFieldDefinition) },
                                                                  { nameof(EDIFieldDefinitionEdit.OnCancel), HandleCancel },
                                                              });
    }

    private async Task DeleteButton_Click(EDIFieldDefinition ediFieldDefinition)
    {
        var confirmation = await DialogService.Confirm("Are you sure you want to delete this EDI Field Definition?", "Delete EDI Field Definition",
                                                       new()
                                                       {
                                                           OkButtonText = "Delete",
                                                           CancelButtonText = "Cancel",
                                                       });

        if (confirmation.GetValueOrDefault())
        {
            var response = await EDIFieldDefinitionService.Delete(ediFieldDefinition);

            if (response.IsSuccessStatusCode)
            {
                NotificationService.Notify(NotificationSeverity.Success, detail: "EDI Field Definition deleted.");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, detail: "Unable to delete EDI Field Definition.");
            }

            await _grid.ReloadGrid();
        }
    }
}
