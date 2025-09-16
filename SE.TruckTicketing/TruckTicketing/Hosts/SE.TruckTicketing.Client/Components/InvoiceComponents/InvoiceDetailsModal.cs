using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Models.Invoices;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class InvoiceDetailsModal : BaseTruckTicketingComponent
{
    [Parameter]
    public Invoice Model { get; set; }

    public async Task OpenModal()
    {
        await DialogService.OpenAsync<InvoiceDetails>($"Invoice {(Model.GlInvoiceNumber.HasText() ? Model.GlInvoiceNumber : Model.ProformaInvoiceNumber)}",
                                                      new() { { nameof(InvoiceDetails.Model), Model } },
                                                      new()
                                                      {
                                                          Height = "80%",
                                                          Width = "80%",
                                                      });
    }
}
