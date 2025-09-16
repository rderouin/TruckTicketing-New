using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class GeneralTab : BaseTruckTicketingComponent
{
    private string Title => string.IsNullOrEmpty(Model.GlInvoiceNumber) ? Model.ProformaInvoiceNumber : Model.ProformaInvoiceNumber + " - " + Model.GlInvoiceNumber;

    [CascadingParameter]
    public Invoice Model { get; set; }

    [Parameter]
    public bool ShowDetailInTitle { get; set; }

    [Parameter]
    public bool HideSalesLines { get; set; }

    private void BeforeLoadingSalesLines(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(SalesLine.InvoiceId), Model?.Id ?? default);
        criteria.AddFilter(nameof(SalesLine.Status), new CompareModel
        {
            Operator = CompareOperators.ne,
            Value = SalesLineStatus.Void.ToString(),
        });
    }
}
