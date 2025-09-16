using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class InvoicePdfRegenDialog : BaseTruckTicketingComponent
{
    private dynamic _dialog;

    private ICollection<Invoice> _invoices;

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public IInvoiceService InvoiceService { get; set; }

    public async Task Process(IEnumerable<SalesLine> salesLines)
    {
        var invoiceNumbers = salesLines.Select(salesLine => salesLine.ProformaInvoiceNumber).ToList();

        var criteria = new SearchCriteriaModel
        {
            Filters =
            {
                [nameof(Invoice.ProformaInvoiceNumber)] = BuildInvoiceNumbersFilter(invoiceNumbers),
                [nameof(Invoice.HasAttachments)] = true,
            },
        };

        var search = await InvoiceService.Search(criteria);

        if (search.Info.TotalRecords == 0)
        {
            return;
        }

        _invoices = search.Results.ToList();

        var parameters = new Dictionary<string, object>
        {
            { nameof(InvoicePdfRegenForm.OnCancel), new EventCallback(this, () => DialogService.Close(_dialog)) },
            { nameof(InvoicePdfRegenForm.OnSubmit), new EventCallback<InvoicePdfRegenFormModel>(this, SendInvoiceRegenRequests) },
        };

        _dialog = await DialogService.OpenAsync<InvoicePdfRegenForm>("Regenerate Invoice PDF", parameters,
                                                                     new()
                                                                     {
                                                                         Width = "500px",
                                                                     });
    }

    private async Task SendInvoiceRegenRequests(InvoicePdfRegenFormModel form)
    {
        var request = new InvoicePdfRegenRequest
        {
            Invoices = _invoices,
            ShowRevisionNumber = form.ShowRevisionNumber,
            IncludeInvoiceCopyWatermark = form.IncludeInvoiceCopyWatermark,
        };

        var response = await InvoiceService.RegenInvoicePdfs(request);

        if (!response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       "Error",
                                       "We were unable to send your request to regenerate invoice PDFs for the modified sales lines");
        }

        DialogService.Close(_dialog);
    }

    private AxiomFilter BuildInvoiceNumbersFilter(IEnumerable<string> invoiceNumbers)
    {
        IJunction query = AxiomFilterBuilder.CreateFilter()
                                            .StartGroup();

        var index = 0;
        foreach (var invoiceNumber in invoiceNumbers)
        {
            var currentAxiom = new Axiom
            {
                Key = $"InvoiceNumber{index++}",
                Field = nameof(Invoice.GlInvoiceNumber),
                Operator = CompareOperators.eq,
                Value = invoiceNumber,
            };

            if (query is GroupStart groupStart)
            {
                query = groupStart.AddAxiom(currentAxiom);
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(currentAxiom);
            }
        }

        return (query as AxiomTokenizer)?.EndGroup().Build();
    }
}
