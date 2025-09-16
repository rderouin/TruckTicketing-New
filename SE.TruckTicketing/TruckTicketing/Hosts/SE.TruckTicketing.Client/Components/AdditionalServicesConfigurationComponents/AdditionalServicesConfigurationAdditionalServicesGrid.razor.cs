using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;

using SE.TruckTicketing.Client.Pages.AdditionalServicesConfiguration;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.AdditionalServicesConfigurationComponents;

public partial class AdditionalServicesConfigurationAdditionalServicesGrid
{
    private SearchResultsModel<AdditionalServicesConfigurationAdditionalService, SearchCriteriaModel> _additionalServices = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<AdditionalServicesConfigurationAdditionalService>(),
    };

    private EditContext _editContext;

    private PagableGridView<AdditionalServicesConfigurationAdditionalService> _grid;

    [Parameter]
    public AdditionalServicesConfiguration Model { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationAdditionalService> OnAdditionalServiceAdd { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationAdditionalService> OnAdditionalServiceChange { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationAdditionalService> OnAdditionalServicesDeleted { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IAdditionalServicesConfigurationService AdditionalServicesConfigurationService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _editContext = new(Model);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        await LoadAdditionalServices();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadAdditionalServices();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnContextChange.InvokeAsync(e.FieldIdentifier);
    }

    private Task LoadAdditionalServices()
    {
        _additionalServices = new(Model.AdditionalServices);
        return Task.CompletedTask;
    }

    private async Task AddAdditionalService(AdditionalServicesConfigurationAdditionalService additionalService)
    {
        DialogService.Close();
        await OnAdditionalServiceAdd.InvokeAsync(additionalService);
    }

    private async Task UpdateAdditionalService(AdditionalServicesConfigurationAdditionalService additionalService)
    {
        DialogService.Close();
        await OnAdditionalServiceChange.InvokeAsync(additionalService);
    }

    private async Task OpenEditDialog(AdditionalServicesConfigurationAdditionalService additionalService, bool isNew)
    {
        var dialogTitleText = string.Join(" ", isNew ? "Add" : "Edit", "Additional Service");
        await DialogService.OpenAsync<AdditionalServicesConfigurationAdditionalServiceEdit>(dialogTitleText,
                                                                                            new()
                                                                                            {
                                                                                                { "Model", Model },
                                                                                                { "AdditionalService", additionalService },
                                                                                                { "IsNewRecord", isNew },
                                                                                                {
                                                                                                    nameof(AdditionalServicesConfigurationAdditionalServiceEdit.AddAdditionalService),
                                                                                                    new EventCallback<AdditionalServicesConfigurationAdditionalService>(this, AddAdditionalService)
                                                                                                },
                                                                                                {
                                                                                                    nameof(AdditionalServicesConfigurationAdditionalServiceEdit.UpdateAdditionalService),
                                                                                                    new EventCallback<AdditionalServicesConfigurationAdditionalService>(this, UpdateAdditionalService)
                                                                                                },
                                                                                                { nameof(AdditionalServicesConfigurationAdditionalServiceEdit.OnCancel), HandleCancel },
                                                                                            });
    }

    private async Task DeleteButton_Click(AdditionalServicesConfigurationAdditionalService additionalService)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Additional Service";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await OnAdditionalServicesDeleted.InvokeAsync(additionalService);
        }
    }
}
