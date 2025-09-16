using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Security;
using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.AdditionalServicesConfiguration;

public partial class AdditionalServicesConfigurationIndexPage : BaseTruckTicketingComponent
{
    private PagableGridView<Contracts.Models.Operations.AdditionalServicesConfiguration> _grid;

    private bool _isLoading;

    private SearchResultsModel<Contracts.Models.Operations.AdditionalServicesConfiguration, SearchCriteriaModel> _searchResults = new();

    private Guid? _updatingId;

    private bool HasAdditionalServicesConfigWritePermission => HasWritePermission(Permissions.Resources.AdditionalServicesConfiguration);

    private string AddAdditionalServicesConfigLink_Css => GetLink_CssClass(HasAdditionalServicesConfigWritePermission);


    [Inject]
    private IServiceProxyBase<Contracts.Models.Operations.AdditionalServicesConfiguration, Guid> AdditionalServicesConfigurationService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadData(new()
        {
            PageSize = 10,
        });
    }

    private async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        _searchResults = await AdditionalServicesConfigurationService.Search(criteria) ?? _searchResults;
        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private async Task UpdateIsActiveState(Guid id, bool isActive)
    {
        _updatingId = id;
        _isLoading = true;
        var response = await AdditionalServicesConfigurationService.Patch(id, new Dictionary<string, object>
        {
            { nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.IsActive), isActive },
        });

        _isLoading = false;
        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Status change successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Status change unsuccessful.");
            var additionalServicesConfiguration = _searchResults?.Results?.FirstOrDefault(additionalServicesConfiguration => additionalServicesConfiguration.Id == id);
            if (additionalServicesConfiguration is not null)
            {
                additionalServicesConfiguration.IsActive = !isActive;
            }
        }

        _updatingId = null;
    }

    private async Task Export()
    {
        var exporter = new PagableGridExporter<Contracts.Models.Operations.AdditionalServicesConfiguration>(_grid, CsvExportService);
        await exporter.Export($"additional-services-configuration-{DateTime.UtcNow:dd-MM-yyyy-hh-mm-ss}.csv");
    }
}
