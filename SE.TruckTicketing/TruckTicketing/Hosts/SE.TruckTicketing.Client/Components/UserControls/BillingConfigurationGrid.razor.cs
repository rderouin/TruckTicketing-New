using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Pages.BillingConfig;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class BillingConfigurationGrid : BaseTruckTicketingComponent
{
    private const string EditBasePath = "/billing-configuration/edit";

    private const string CloneBasePath = "/billing-configuration/clone";

    private const string AddBasePath = "/billing-configuration/new/";

    private readonly List<ListOption<int>> _activeBillingConfigurationListBoxData = new()
    {
        new()
        {
            Value = 1,
            Display = "Active",
        },
        new()
        {
            Value = 2,
            Display = "In-Active",
        },
    };

    private string _addNewBillingConfigurationPath;

    private string AddBillingConfigLink_Css => GetLink_CssClass((HasWritePermission(Permissions.Resources.Account)));

    private SearchResultsModel<BillingConfiguration, SearchCriteriaModel> _billingConfigurations = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<BillingConfiguration>(),
    };

    private bool _ediFieldsLoading;

    private PagableGridView<BillingConfiguration> _grid;

    private GridFiltersContainer _gridFilterContainer;

    //local fields
    private bool _isLoading;

    private SearchResultsModel<EDIFieldDefinition, SearchCriteriaModel> _loadEDIFieldDefinition = new();

    [Inject]
    private IServiceBase<BillingConfiguration, Guid> BillingConfigurationService { get; set; }

    [Parameter]
    public Guid? BillingCustomerId { get; set; }

    [Parameter]
    public Guid? InvoiceConfigurationId { get; set; }

    [Parameter]
    public bool DisplayAddButton { get; set; }

    [Parameter]
    public bool DisplayFilters { get; set; }

    [Parameter]
    public bool IsInvoiceConfigurationCloned { get; set; }

    [Parameter]
    public List<BillingConfiguration> InvalidBillingConfigurations { get; set; }

    [Parameter]
    public List<BillingConfiguration> ClonedBillingConfigurations { get; set; }

    [Parameter]
    public InvoiceConfiguration ClonedInvoiceConfiguration { get; set; }

    [Inject]
    private IEDIFieldDefinitionService EDIFieldDefinitionService { get; set; }

    //Services
    [Inject]
    private IBillingConfigurationService BillingService { get; set; }

    protected string ClassNames(params (string className, bool include)[] classNames)
    {
        var classes = string.Join(" ", (classNames ?? Array.Empty<(string className, bool include)>()).Where(_ => _.include).Select(_ => _.className));
        return $"{classes}";
    }

    public string GetEDIValue(Guid ediFieldDefinitionID, BillingConfiguration billingConfiguration)
    {
        if (billingConfiguration.EDIValueData != null && billingConfiguration.EDIValueData.Count > 0)
        {
            var ediValue = billingConfiguration.EDIValueData.Where(x => x.EDIFieldDefinitionId == ediFieldDefinitionID)
                                               .FirstOrDefault(new EDIFieldValue());

            if (ediValue.Id != default)
            {
                return ediValue.EDIFieldValueContent;
            }
        }

        return string.Empty;
    }

    public async Task ReloadBillingConfigurationGrid()
    {
        if (_grid != null)
        {
            await _grid.ReloadGrid();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _ediFieldsLoading = true;
        _addNewBillingConfigurationPath = String.Concat(AddBasePath, BillingCustomerId);
        //Load EDIFields using BillingCustomerId
        await LoadData(new() { PageSize = 10 });

        await LoadEDIFieldDefinitionAsync(BillingCustomerId);

        _ediFieldsLoading = false;
    }

    private void BeforeGeneratorFilterDataLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.Generator.ToString(),
        };

        criteria.Filters[nameof(Account.IsAccountActive)] = true;
    }

    private async Task LoadEDIFieldDefinitionAsync(Guid? id)
    {
        var current = new SearchCriteriaModel
        {
            PageSize = 10,
            CurrentPage = 0,
            Keywords = "",
            Filters = new(),
        };

        current.Filters.TryAdd(nameof(EDIFieldDefinition.CustomerId), id);
        var results = await EDIFieldDefinitionService.Search(current);

        _loadEDIFieldDefinition.Results = results?.Results ?? new List<EDIFieldDefinition>();
        _loadEDIFieldDefinition.Info = results?.Info ?? new();
    }

    private async Task LoadData(SearchCriteriaModel searchCriteria)
    {
        _isLoading = true;

        if (IsInvoiceConfigurationCloned)
        {
            var clonedBillingConfigurations = ClonedBillingConfigurations != null && ClonedBillingConfigurations.Any() ? ClonedBillingConfigurations.ToList() : new();
            var morePages = searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault() < clonedBillingConfigurations.Count;
            var results = new SearchResultsModel<BillingConfiguration, SearchCriteriaModel>
            {
                Results = clonedBillingConfigurations
                         .Skip(searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault())
                         .Take(searchCriteria.PageSize.GetValueOrDefault()),
                Info = new()
                {
                    TotalRecords = clonedBillingConfigurations.Count,
                    NextPageCriteria = morePages ? new SearchCriteriaModel { CurrentPage = searchCriteria.CurrentPage + 1 } : null,
                },
            };

            _billingConfigurations = results;
        }
        else
        {
            searchCriteria.Filters.TryAdd(nameof(BillingConfiguration.BillingCustomerAccountId), BillingCustomerId);
            if (InvoiceConfigurationId != null || InvoiceConfigurationId != default)
            {
                searchCriteria.Filters.TryAdd(nameof(BillingConfiguration.InvoiceConfigurationId), InvoiceConfigurationId);
            }

            var results = await BillingConfigurationService.Search(searchCriteria);
            _billingConfigurations.Results = results?.Results ?? new List<BillingConfiguration>();
            _billingConfigurations.Info = results?.Info ?? new();
        }

        if (InvalidBillingConfigurations != null && InvalidBillingConfigurations.Any())
        {
            foreach (var billingConfigurationsResult in InvalidBillingConfigurations)
            {
                _billingConfigurations.Results.First(x => x.Id == billingConfigurationsResult.Id).IsValid = false;
            }
        }

        _isLoading = false;
    }

    private async Task EditButton_Click(BillingConfiguration model)
    {
        if (!DisplayAddButton)
        {
            var operation = IsInvoiceConfigurationCloned ? "clone" : "edit";
            var label = IsInvoiceConfigurationCloned ? "Clone" : "Edit";

            await DialogService.OpenAsync<BillingConfigurationEdit>($"{label} Billing Configuration", new()
            {
                { nameof(BillingConfigurationEdit.Id), model.Id },
                { nameof(BillingConfigurationEdit.BillingConfigurationModel), model },
                { nameof(BillingConfigurationEdit.InvoiceConfigurationModel), ClonedInvoiceConfiguration },
                { nameof(BillingConfigurationEdit.BillingCustomerId), model.BillingCustomerAccountId.ToString() },
                { nameof(BillingConfigurationEdit.InvoiceConfigurationId), model.InvoiceConfigurationId },
                { nameof(BillingConfigurationEdit.IsInvoiceConfigurationCloned), IsInvoiceConfigurationCloned },
                { nameof(BillingConfigurationEdit.Operation), operation },
                { nameof(BillingConfigurationEdit.AddEditBillingConfiguration), new EventCallback<BillingConfiguration>(this, BillingConfigurationGridReload) },
                { nameof(BillingConfigurationEdit.CancelAddEditBillingConfiguration), new EventCallback<bool>(this, CancelBillingConfigurationAddEdit) },
            }, new()
            {
                Width = "80%",
                Height = "95%",
            });
        }
        else
        {
            await NavigateEditPage(model);
        }
    }

    private async Task CloneButton_Click(BillingConfiguration model)
    {
        var clonedModel = model.Clone();
        clonedModel.Name = $"(COPY) {model.Name}";
        clonedModel.Id = Guid.NewGuid();
        if (!DisplayAddButton)
        {
            var operation = "clone";
            await DialogService.OpenAsync<BillingConfigurationEdit>("Clone Billing Configuration", new()
            {
                { nameof(BillingConfigurationEdit.Id), clonedModel.Id },
                { nameof(BillingConfigurationEdit.BillingConfigurationModel), clonedModel },
                { nameof(BillingConfigurationEdit.InvoiceConfigurationModel), ClonedInvoiceConfiguration },
                { nameof(BillingConfigurationEdit.BillingCustomerId), clonedModel.BillingCustomerAccountId.ToString() },
                { nameof(BillingConfigurationEdit.InvoiceConfigurationId), clonedModel.InvoiceConfigurationId },
                { nameof(BillingConfigurationEdit.IsInvoiceConfigurationCloned), IsInvoiceConfigurationCloned },
                { nameof(BillingConfigurationEdit.Operation), operation },
                { nameof(BillingConfigurationEdit.AddEditBillingConfiguration), new EventCallback<BillingConfiguration>(this, BillingConfigurationGridReload) },
                { nameof(BillingConfigurationEdit.CancelAddEditBillingConfiguration), new EventCallback<bool>(this, CancelBillingConfigurationAddEdit) },
            }, new()
            {
                Width = "80%",
                Height = "95%",
            });
        }
        else
        {
            await NavigateEditPage(clonedModel);
        }
    }

    private async Task BillingConfigurationGridReload(BillingConfiguration billingConfigurationAddEdit)
    {
        if (ClonedBillingConfigurations.All(x => x.Id != billingConfigurationAddEdit.Id))
        {
            ClonedBillingConfigurations.Add(billingConfigurationAddEdit);
        }

        ClonedBillingConfigurations.First(x => x.Id == billingConfigurationAddEdit.Id).IsValid = true;
        DialogService.Close();
        await _grid.ReloadGrid();
    }

    private async Task CancelBillingConfigurationAddEdit(bool isCanceled)
    {
        DialogService.Close();
        await _grid.ReloadGrid();
    }

    private Task NavigateEditPage(BillingConfiguration model)
    {
        NavigationManager.NavigateTo($"{EditBasePath}/{BillingCustomerId}/{model.Id}");
        return Task.CompletedTask;
    }

    private Task NavigateClonePage(BillingConfiguration model)
    {
        NavigationManager.NavigateTo($"{CloneBasePath}/{BillingCustomerId}/{model.Id}");
        return Task.CompletedTask;
    }

    private Task AddNewBillingConfiguration()
    {
        NavigationManager.NavigateTo($"{AddBasePath}/{BillingCustomerId}");
        return Task.CompletedTask;
    }

    private async void OnDefaultBillngConfigurationChange(bool isDefault, BillingConfiguration billingConfiguration)
    {
        //Apply Patch for current context
        await BillingConfigurationService.Patch(billingConfiguration.Id, new Dictionary<string, object> { [nameof(BillingConfiguration.IsDefaultConfiguration)] = isDefault });
        await _grid.ReloadGrid();
    }
}
