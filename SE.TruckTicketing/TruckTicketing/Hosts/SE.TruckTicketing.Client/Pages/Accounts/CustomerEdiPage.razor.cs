using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.Accounts;

public partial class CustomerEdiPage : BaseTruckTicketingComponent
{
    private bool IsLoadingInvoiceExchanges { get; set; }

    private Account Customer { get; set; }

    private SearchResultsModel<InvoiceExchangeDto, SearchCriteriaModel> InvoiceExchanges { get; set; } = new();

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    public IServiceProxyBase<Account, Guid> AccountService { get; set; }

    [Inject]
    public IServiceProxyBase<InvoiceExchangeDto, Guid> InvoiceExchangeService { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;

        if (Id == Guid.Empty || Customer?.Id != Id)
        {
            Customer = await AccountService.GetById(Id);
        }

        IsLoading = false;
    }

    protected async Task LoadInvoiceExchanges(SearchCriteriaModel criteria)
    {
        IsLoadingInvoiceExchanges = true;

        criteria.Filters[nameof(InvoiceExchangeDto.BillingAccountId)] = Customer.Id;

        InvoiceExchanges = await InvoiceExchangeService.Search(criteria) ?? InvoiceExchanges;

        IsLoadingInvoiceExchanges = false;
    }
}
