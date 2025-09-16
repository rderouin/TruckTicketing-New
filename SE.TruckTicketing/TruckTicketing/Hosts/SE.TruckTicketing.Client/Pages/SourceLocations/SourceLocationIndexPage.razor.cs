using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.Accounts;
using SE.TruckTicketing.Client.Components.Accounts.Edit;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.SourceLocations;

public partial class SourceLocationIndexPage : BaseTruckTicketingComponent
{
    private bool _disableSourceLocationReassignment = true;

    private PagableGridView<SourceLocation> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private Response<SourceLocation> _sourceLocationReAssociationResponse = new();

    private SearchResultsModel<SourceLocation, SearchCriteriaModel> _sourceLocations = new();

    private DataGridSelectionMode _selectionMode => EnableMultiLineSelect ? DataGridSelectionMode.Multiple : DataGridSelectionMode.Single;

    private SourceLocationReassignment SourceLocationOwner { get; } = new();

    public string SourceLocationSuccessfulReAssociationNotificationMessage => "Selected Source Locations Re-Associated";

    public string SourceLocationFailReAssociationNotificationMessage => "Selected Source Locations Failed to Re-Associate";

    [Parameter]
    public bool DisplayAddButton { get; set; } = true;

    [Parameter]
    public bool EnableMultiLineSelect { get; set; } = true;

    [Parameter]
    public EventCallback<SearchCriteriaModel> OnDataLoading { get; set; }

    [Parameter]
    public RenderFragment Columns { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    [Inject]
    private IServiceProxyBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    
    private bool HasSourceLocationWritePermission => HasWritePermission(Permissions.Resources.SourceLocation);

    private string AddSourceLocationLink_Css => GetLink_CssClass(HasSourceLocationWritePermission);

    protected async Task Export()
    {
        var exporter = new PagableGridExporter<SourceLocation>(_grid, CsvExportService);
        await exporter.Export("source-locations.csv");
    }

    protected void BeforeGeneratorFilterDataLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.Generator.ToString(),
        };
    }

    protected async Task LoadData(SearchCriteriaModel current)
    {
        _isLoading = true;
        StateHasChanged();
        await BeforeDataLoad(current);
        current.Filters[nameof(SourceLocation.IsDeleted)] = false;
        _sourceLocations = await SourceLocationService.Search(current) ?? _sourceLocations;
        _isLoading = false;
        StateHasChanged();
    }

    protected virtual async Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        if (OnDataLoading.HasDelegate)
        {
            await OnDataLoading.InvokeAsync(criteria);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (DisplayAddButton)
            {
                _gridFilterContainer.Reload();
            }
            else
            {
                await _grid.ReloadGrid();
            }
        }
    }

    private async Task ReassignSourceLocations()
    {
        //Open Dialog to re-assign selected SourceLocations to new Generator
        if (_grid.SelectedResults != null && _grid.SelectedResults.Any())
        {
            await DialogService.OpenAsync<ReAssignSourceLocations>("Re-Assign Source Location(s)",
                                                                   new()
                                                                   {
                                                                       { nameof(ReAssignSourceLocations.SourceLocationOwnerModel), SourceLocationOwner },
                                                                       { nameof(ReAssignSourceLocations.OnCreateNewGenerator), new EventCallback(this, CreateNewGenerator) },
                                                                       { nameof(ReAssignSourceLocations.OnSubmit), new EventCallback(this, HandleSelectedGenerator) },
                                                                       { nameof(ReAssignSourceLocations.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                   });
        }

        await Task.CompletedTask;
    }

    private async Task CreateNewGenerator()
    {
        DialogService.Close();
        await DialogService.OpenAsync<NewAccountDialog>("New Generator", new()
        {
            { nameof(NewAccountDialog.AccountType), AccountTypes.Generator.ToString() },
            { nameof(NewAccountDialog.AddAccount), new EventCallback<Account>(this, NewGeneratorCreated) },
        }, new()
        {
            Width = "80%",
            Height = "80%",
        });

        await Task.CompletedTask;
    }

    private async Task HandleSelectedGenerator()
    {
        //ReAssign selected Source Locations to selected generator
        DialogService.Close();
        await UpdateSourceLocationAssociation(SourceLocationOwner.SelectedGenerator);
        SourceLocationOwner.SelectedGenerator = new();
        SourceLocationOwner.OwnershipStartDate = null;
    }

    private async Task NewGeneratorCreated()
    {
        //ReAssign selected Source Locations to new generator
        DialogService.Close();
        await ReassignSourceLocations();
    }

    private async Task UpdateSourceLocationAssociation(Account account)
    {
        IEnumerable<AccountContact> productionAccountant =
            account?
               .Contacts?
               .Where(c => c.ContactFunctions.Contains(AccountContactFunctions.ProductionAccountant.ToString()))
               .ToList() ?? new();

        //Update Ownership history for updated Generator and new created/selected Generator
        foreach (var sourceLocation in _grid.SelectedResults)
        {
            var newOwnershipStartDate = SourceLocationOwner.OwnershipStartDate ?? default;
            var oldGeneratorId = sourceLocation.GeneratorId;
            var oldContractOperatorId = sourceLocation.ContractOperatorId;
            if (account != null && account.Id != Guid.Empty)
            {
                sourceLocation.GeneratorId = account.Id;
                sourceLocation.GeneratorName = account.Name;
                sourceLocation.GeneratorStartDate = newOwnershipStartDate;

                sourceLocation.GeneratorProductionAccountContactId = productionAccountant.Any() && productionAccountant.Count() == 1 ? productionAccountant.First().Id : null;
                if (oldContractOperatorId == oldGeneratorId)
                {
                    sourceLocation.ContractOperatorId = account.Id;
                    sourceLocation.ContractOperatorName = account.Name;
                    sourceLocation.ContractOperatorProductionAccountContactId = sourceLocation.GeneratorProductionAccountContactId;
                }
            }

            var response = await SourceLocationService.Update(sourceLocation);
            _sourceLocationReAssociationResponse = response;
            if (_sourceLocationReAssociationResponse.IsSuccessStatusCode)
            {
                continue;
            }

            NotificationService.Notify(NotificationSeverity.Error, detail: SourceLocationFailReAssociationNotificationMessage);
            break;
        }

        if (_sourceLocationReAssociationResponse.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: SourceLocationSuccessfulReAssociationNotificationMessage);
        }

        await _grid.ReloadGrid();
    }

    private string GetRegistryUrls(string registryName)
    {
        return registryName switch
               {
                   "MBOGC" => PetroleumRegistryLinks.MBOGC,
                   "NDIC" => PetroleumRegistryLinks.NDIC,
                   "IRIS" => PetroleumRegistryLinks.IRIS,
                   "BCOGC" => PetroleumRegistryLinks.BCOGC,
                   "PETRINEX" => PetroleumRegistryLinks.PETRINEX,
                   _ => String.Empty,
               };
    }

    private void SourceLocationsSelected()
    {
        _disableSourceLocationReassignment = !(_grid.SelectedResults != null && _grid.SelectedResults.Any());
    }
}

public class SourceLocationReassignment
{
    public Account SelectedGenerator { get; set; } = new();

    public DateTimeOffset? OwnershipStartDate { get; set; }
}
