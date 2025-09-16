using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.InvoiceConfig;

public partial class InvoiceConfigurationIndexPage : BaseRazorComponent
{
    private const string EditBasePath = "/invoice-configurations/edit";

    private const string CloneBasePath = "/invoice-configurations/clone";

    private PagableGridView<InvoiceConfiguration> _grid;

    private SearchResultsModel<InvoiceConfiguration, SearchCriteriaModel> _invoiceConfigurations = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<InvoiceConfiguration>(),
    };

    private bool _isLoading;

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceBase<InvoiceConfiguration, Guid> InvoiceConfigurationService { get; set; }

    [Parameter]
    public Guid? BillingCustomerId { get; set; }

    [Parameter]
    public bool DisplayAddButton { get; set; } = true;

    [Parameter]
    public bool IsSaveButtonDisabled { get; set; } 

    private async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        if (!DisplayAddButton)
        {
            criteria.Filters.TryAdd(nameof(InvoiceConfiguration.CustomerId), BillingCustomerId);
        }

        _invoiceConfigurations = await InvoiceConfigurationService.Search(criteria) ?? _invoiceConfigurations;
        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private void EditButton_Click(InvoiceConfiguration model)
    {
        NavigateEditPage(model);
    }

    private async Task CloneButton_Click(InvoiceConfiguration model)
    {
        var navigationRoute = $"{CloneBasePath}/{model.Id}/";
        if (BillingCustomerId != default)
        {
            navigationRoute = string.Concat(navigationRoute, BillingCustomerId);
        }

        NavigationManager.NavigateTo(navigationRoute);
        await Task.CompletedTask;
    }

    private void NavigateEditPage(InvoiceConfiguration model)
    {
        var navigationRoute = $"{EditBasePath}/{model.Id}/";
        if (BillingCustomerId != default)
        {
            navigationRoute = string.Concat(navigationRoute, BillingCustomerId);
        }

        NavigationManager.NavigateTo(navigationRoute);
    }

    protected string ClassNames(params (string className, bool include)[] classNames)
    {
        var classes = string.Join(" ", (classNames ?? Array.Empty<(string className, bool include)>()).Where(_ => _.include).Select(_ => _.className));
        return $"{classes}";
    }
}
