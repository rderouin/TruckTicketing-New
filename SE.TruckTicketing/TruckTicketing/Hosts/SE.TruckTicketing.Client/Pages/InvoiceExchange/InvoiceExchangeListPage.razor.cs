using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Enums;

namespace SE.TruckTicketing.Client.Pages.InvoiceExchange;

public partial class InvoiceExchangeListPage
{
    [Inject]
    public IServiceBase<InvoiceExchangeDto, Guid> InvoiceExchangeService { get; set; }

    private SearchCriteriaModel SearchCriteriaModel { get; set; } = new()
    {
        OrderBy = nameof(InvoiceExchangeDto.PlatformCode),
        SortOrder = SortOrder.Asc,
    };

    private SearchResultsModel<InvoiceExchangeDto, SearchCriteriaModel> SearchResults { get; set; } = new();

    private bool IsGridLoading { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await WithLoadingScreen(async () =>
                                {
                                    await RefreshGrid();
                                    await base.OnInitializedAsync();
                                });
    }

    private async Task RefreshGrid()
    {
        try
        {
            IsGridLoading = true;
            SearchResults = await InvoiceExchangeService.Search(SearchCriteriaModel) ?? new();
        }
        finally
        {
            IsGridLoading = false;
        }
    }

    private async Task RowSplitButtonClick(RadzenSplitButtonItem item, InvoiceExchangeDto model)
    {
        switch (item?.Value)
        {
            case "delete":
                await DeleteConfig(model);
                return;

            case "clone":
                CloneConfig(model);
                return;

            case nameof(InvoiceExchangeType.BusinessStream):
                OpenConfigPage(InvoiceExchangeType.BusinessStream, null, model.Id);
                return;

            case nameof(InvoiceExchangeType.LegalEntity):
                OpenConfigPage(InvoiceExchangeType.LegalEntity, null, model.Id);
                return;

            case nameof(InvoiceExchangeType.Customer):
                OpenConfigPage(InvoiceExchangeType.Customer, null, model.Id);
                return;

            default:
                OpenConfigPage(model.Type, model.Id, null);
                return;
        }
    }

    private async Task DeleteConfig(InvoiceExchangeDto model)
    {
        var confirmed = await DialogService.Confirm("Are you sure you want to delete this configuration?", "Confirm deletion", new()
        {
            OkButtonText = "Delete",
            CancelButtonText = "Cancel",
        });

        if (confirmed == true)
        {
            await InvoiceExchangeService.Patch(model.Id, new Dictionary<string, object> { [nameof(InvoiceExchangeDto.IsDeleted)] = true });
            await RefreshGrid();
        }
    }

    private void CloneConfig(InvoiceExchangeDto model)
    {
        NavigationManager.NavigateTo($"/invoice-exchanges/{(int)model.Type}/clone/{model.Id}");
    }

    private Task OnDataGridLoad(SearchCriteriaModel searchCriteriaModel)
    {
        SearchCriteriaModel = searchCriteriaModel;
        return RefreshGrid();
    }

    private void OpenConfigPage(InvoiceExchangeType type, Guid? id, Guid? originalId)
    {
        var path = $"/invoice-exchanges/{(int)type}";
        path += id.HasValue ? $"/{id}" : type == InvoiceExchangeType.Global ? "/add" : $"/override/{originalId}";
        NavigationManager.NavigateTo(path);
    }
}
